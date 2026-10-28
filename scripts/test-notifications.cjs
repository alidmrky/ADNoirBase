// Run against a LOCAL API with Log/Pickup providers only.
// Creates uniquely named test users, templates and delivery records.
const fs = require("node:fs");
const crypto = require("node:crypto");
const assert = require("node:assert/strict");
const { HubConnectionBuilder, LogLevel } = require("../ui/node_modules/@microsoft/signalr");
const config = Object.fromEntries(fs.readFileSync(require("node:path").join(__dirname, "../.env"), "utf8").split(/\r?\n/).filter(l => l && !l.startsWith("#") && l.includes("=")).map(l => [l.slice(0,l.indexOf("=")),l.slice(l.indexOf("=")+1)]));
if (config.Notification__Email__Provider === "Smtp") throw Error("Use a local Log/Pickup provider for this smoke test.");
const base = "http://localhost:5197";
const tag = crypto.randomBytes(5).toString("hex");
const email = "notification-test-" + tag + "@example.invalid";
const password = crypto.randomBytes(24).toString("base64");
const checks = [];
let admin, user, hub;
async function request(path, token, method = "GET", body, expected = 200) {
  const response = await fetch(base + "/api/" + path, {
    method, headers: { ...(token ? { Authorization: "Bearer " + token } : {}), ...(body instanceof FormData ? {} : { "Content-Type": "application/json" }) },
    body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body),
  });
  const text = await response.text();
  assert.equal(response.status, expected, path + ": " + text.slice(0, 250));
  return text ? JSON.parse(text) : null;
}
function pass(name) { checks.push(name); console.log("PASS " + name); }
async function until(action, predicate, timeout = 45000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const value = await action();
    if (predicate(value)) return value;
    await new Promise(r => setTimeout(r, 2000));
  }
  throw Error("Timed out waiting for background delivery");
}
(async () => {
  admin = (await request("auth/login", null, "POST", { email: config.ADMIN_EMAIL, password: config.ADMIN_PASSWORD })).data;
  await request("auth/register", null, "POST", { email, password, confirmPassword: password, firstName: "Notification", lastName: "Smoke" }, 201);
  user = (await request("auth/login", null, "POST", { email, password })).data;
  await request("admin/notifications/templates", null, "GET", undefined, 401);
  await request("admin/notifications/templates", user.accessToken, "GET", undefined, 403);
  await request("notifications", null, "GET", undefined, 401);
  pass("Anonymous and non-admin authorization");
  await until(() => request("admin/notifications/messages?recipient=" + encodeURIComponent(email), admin.accessToken), r => r.data.items.some(m => m.templateCode === "identity.welcome" && m.status === "Sent"));
  pass("Registration event -> welcome message -> delivered");
  await request("user/me/change-password", user.accessToken, "PUT", { currentPassword: password, newPassword: password + "A1!", confirmNewPassword: password + "A1!" });
  await until(() => request("admin/notifications/messages?recipient=" + encodeURIComponent(email), admin.accessToken), r => r.data.items.some(m => m.templateCode === "identity.password_changed" && m.status === "Sent"));
  pass("Password change event -> notification delivered");
  let form = new FormData(); form.set("file", new Blob(["x"], { type: "application/x-msdownload" }), "bad.exe");
  await request("admin/notifications/attachments", admin.accessToken, "POST", form, 422);
  form = new FormData(); form.set("file", new Blob([new Uint8Array(10485761)], { type: "text/plain" }), "large.txt");
  await request("admin/notifications/attachments", admin.accessToken, "POST", form, 422);
  form = new FormData(); form.set("file", new Blob(["local smoke test"], { type: "text/plain" }), "smoke.txt");
  const attachment = (await request("admin/notifications/attachments", admin.accessToken, "POST", form, 201)).data;
  const manual = { channel: "Email", to: [{ address: email, userId: user.userId }], subject: "Local test", textBody: "Test", attachmentIds: [attachment.fileId] };
  await request("admin/notifications/send", admin.accessToken, "POST", { ...manual, attachmentIds: Array(11).fill(attachment.fileId) }, 422);
  await request("admin/notifications/send", admin.accessToken, "POST", { ...manual, attachmentIds: ["invalid"] }, 422);
  await request("admin/notifications/send", admin.accessToken, "POST", manual);
  pass("Attachment type, byte limit, count and stored-reference validation");
  const created = (await request("admin/notifications/templates", admin.accessToken, "POST", {
    code: "smoke." + tag, name: "Smoke " + tag, channel: "Email", category: "Marketing", isLayout: false,
    subject: "Hello {{ full_name }}", htmlBody: "<p>{{ full_name }}</p>", textBody: "Hello {{ full_name }}", logPolicy: "Full", retentionDays: 1
  }, 201)).data;
  const preview = (await request("admin/notifications/templates/preview", admin.accessToken, "POST", {
    subject: "Preview", htmlBody: "<p>{{ value }}</p>", sampleValues: { value: "<script>alert(1)</script>" }
  })).data;
  assert(!preview.htmlBody.includes("<script>"));
  pass("Template create and escaped preview");
  const connection = new HubConnectionBuilder().withUrl(base + "/hubs/notifications", { accessTokenFactory: () => user.accessToken }).configureLogging(LogLevel.None).build();
  hub = connection;
  let live;
  connection.on("NotificationReceived", payload => { live = payload; });
  await connection.start();
  await request("admin/notifications/send", admin.accessToken, "POST", { channel: "InApp", to: [{ address: user.userId, userId: user.userId }], subject: "Live " + tag, textBody: "Realtime test" });
  await until(async () => live, Boolean);
  const inbox = (await request("notifications", user.accessToken)).data;
  const item = inbox.items.find(n => n.title === "Live " + tag);
  assert(item);
  await request("notifications/" + item.id + "/read", admin.accessToken, "POST", undefined, 404);
  await request("notifications/" + item.id + "/read", user.accessToken, "POST");
  assert.equal((await request("notifications/unread-count", user.accessToken)).data, 0);
  pass("SignalR delivery, inbox ownership and unread counter");
  await request("admin/notifications/send", admin.accessToken, "POST", { ...manual, category: "Marketing", templateCode: created.code, attachmentIds: [] });
  const token = crypto.createHmac("sha256", config.Notification__UnsubscribeSecret).update(user.userId.replaceAll("-", "")).digest("base64url");
  await request("notifications/unsubscribe", null, "POST", { token });
  await request("notifications/unsubscribe", null, "POST", { token });
  const prefs = (await request("notifications/preferences", user.accessToken)).data;
  assert.equal(prefs.emailMarketing, false);
  const skipped = (await request("admin/notifications/send", admin.accessToken, "POST", { ...manual, category: "Marketing", attachmentIds: [] })).data;
  assert.equal(skipped.queuedMessageIds.length, 0);
  pass("Unsubscribe is idempotent and marketing preferences are enforced");
  const campaign = (await request("admin/notifications/campaigns", admin.accessToken, "POST", {
    name: "Smoke " + tag, channel: "Email", templateCode: created.code, audience: { type: "ManualAddresses", manualAddresses: [email] }
  }, 201)).data;
  await request("admin/notifications/campaigns/" + campaign.id + "/start", admin.accessToken, "POST");
  await until(() => request("admin/notifications/campaigns/" + campaign.id, admin.accessToken), r => r.data.status === "Completed");
  pass("Campaign expansion and completion");
  await request("admin/notifications/campaigns/" + campaign.id, admin.accessToken, "DELETE");
  await request("admin/notifications/templates/" + created.id, admin.accessToken, "DELETE");
  await request("admin/users/" + user.userId + "/deactivate", admin.accessToken, "POST");
  pass("Test campaign/template removed and test account deactivated");
  console.log(JSON.stringify({ passed: checks.length, checks }, null, 2));
})().catch(e => { console.error(e.message); process.exitCode = 1; }).finally(async () => { if (hub) await hub.stop(); });


