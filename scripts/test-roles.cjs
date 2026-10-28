// Local API smoke test. Test roles are deleted and the test user is deactivated.
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const assert = require("node:assert/strict");
const config = Object.fromEntries(fs.readFileSync(path.join(__dirname, "../.env"), "utf8").split(/\r?\n/).filter(l => l && !l.startsWith("#") && l.includes("=")).map(l => [l.slice(0,l.indexOf("=")), l.slice(l.indexOf("=")+1)]));
const base = "http://localhost:5197/api/";
const tag = crypto.randomBytes(5).toString("hex");
const keep = process.argv.includes("--keep-ui-user");
const fixturePath = path.join(__dirname, "../.git/role-ui-fixture.json");
let admin, user, userId, adminRole, fixture;
const createdRoles = [];
const checks = [];
async function call(route, token, method = "GET", body, expected = 200) {
  const response = await fetch(base + route, { method, headers: { "Content-Type": "application/json", ...(token ? { Authorization: "Bearer " + token } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  assert.equal(response.status, expected, route + ": " + text.slice(0, 250));
  return text ? JSON.parse(text).data : undefined;
}
function pass(message) { checks.push(message); console.log("PASS " + message); }
(async () => {
  admin = await call("auth/login", null, "POST", { email: config.ADMIN_EMAIL, password: config.ADMIN_PASSWORD });
  const catalog = await call("admin/roles", admin.accessToken);
  adminRole = catalog.items.find(r => r.code === "ADMIN");
  assert(adminRole?.isSystem);
  if (process.argv.includes("--cleanup-ui-user")) {
    const saved = JSON.parse(fs.readFileSync(fixturePath, "utf8"));
    await call("admin/users/" + saved.id + "/roles/" + adminRole.id, admin.accessToken, "DELETE");
    await call("admin/users/" + saved.id + "/deactivate", admin.accessToken, "POST");
    fs.unlinkSync(fixturePath);
    pass("UI test admin access removed and account deactivated");
    return;
  }
  const email = "roles-test-" + tag + "@example.invalid";
  const password = "Test!" + crypto.randomBytes(24).toString("base64");
  const created = await call("admin/users", admin.accessToken, "POST", { email, password, firstName: "Role", lastName: "Test " + tag }, 201);
  userId = created.id;
  fixture = { email, password, id: userId };
  user = await call("auth/login", null, "POST", { email, password });
  await call("admin/roles", null, "GET", undefined, 401);
  await call("admin/roles", user.accessToken, "GET", undefined, 403);
  await call("admin/roles", user.accessToken, "POST", { name: "Forbidden", code: "FORBIDDEN" }, 403);
  pass("Role endpoints require ADMIN");
  await call("admin/roles", admin.accessToken, "POST", { name: " ", code: "BAD" }, 422);
  const role = await call("admin/roles", admin.accessToken, "POST", { name: "Role " + tag, code: " test_" + tag + " ", description: "smoke" }, 201);
  createdRoles.push(role.id);
  assert.equal(role.code, ("test_" + tag).toUpperCase());
  await call("admin/roles", admin.accessToken, "POST", { name: "Duplicate", code: role.code.toLowerCase() }, 409);
  await call("admin/roles/" + role.id, admin.accessToken, "PUT", { name: "Updated " + tag, description: "changed" });
  assert.equal((await call("admin/roles/" + role.id, admin.accessToken)).name, "Updated " + tag);
  assert((await call("admin/roles?searchTerm=" + tag, admin.accessToken)).items.some(r => r.id === role.id));
  pass("Role create, normalization, duplicate, update and search");
  await call("admin/roles/" + adminRole.id, admin.accessToken, "PUT", { name: "Changed" }, 422);
  await call("admin/roles/" + adminRole.id, admin.accessToken, "DELETE", undefined, 422);
  await call("admin/users/" + admin.userId + "/roles/" + adminRole.id, admin.accessToken, "DELETE", undefined, 422);
  pass("System roles and own ADMIN access protected");
  const assignRoute = "admin/users/" + userId + "/roles/" + role.id;
  await call(assignRoute, admin.accessToken, "POST");
  await call(assignRoute, admin.accessToken, "POST", undefined, 409);
  assert((await call("admin/users/" + userId + "/roles", admin.accessToken)).some(r => r.id === role.id));
  assert.equal((await call("admin/roles/" + role.id, admin.accessToken)).userCount, 1);
  await call("admin/roles/" + role.id, admin.accessToken, "DELETE", undefined, 409);
  const fresh = await call("auth/login", null, "POST", { email, password });
  assert(fresh.roles.includes(role.code));
  pass("Assignment persists; duplicate and in-use deletion rejected; new token has role");
  await call(assignRoute, admin.accessToken, "DELETE");
  await call(assignRoute, admin.accessToken, "DELETE", undefined, 409);
  const responses = await Promise.all([1,2].map(() => fetch(base + assignRoute, { method: "POST", headers: { Authorization: "Bearer " + admin.accessToken } })));
  assert.deepEqual(responses.map(r => r.status).sort(), [200,409]);
  assert.equal((await call("admin/roles/" + role.id, admin.accessToken)).userCount, 1);
  pass("Concurrent assignment produces one row and 200/409, never 500");
  await call("admin/users/" + crypto.randomUUID() + "/roles/" + role.id, admin.accessToken, "POST", undefined, 404);
  await call("admin/users/" + userId + "/roles/" + crypto.randomUUID(), admin.accessToken, "POST", undefined, 404);
  await call("admin/users/" + userId + "/roles/" + crypto.randomUUID(), admin.accessToken, "DELETE", undefined, 404);
  pass("Missing user and role return 404");
  if (keep) {
    await call("admin/users/" + userId + "/roles/" + adminRole.id, admin.accessToken, "POST");
    fs.writeFileSync(fixturePath, JSON.stringify(fixture));
    console.log("UI fixture saved in .git/role-ui-fixture.json");
  }
  console.log(JSON.stringify({ passed: checks.length, checks }, null, 2));
})().catch(e => { console.error(e.message); process.exitCode = 1; }).finally(async () => {
  if (!admin || !userId) return;
  for (const roleId of createdRoles) {
    const assigned = await call("admin/users/" + userId + "/roles", admin.accessToken);
    if (assigned.some(r => r.id === roleId)) await call("admin/users/" + userId + "/roles/" + roleId, admin.accessToken, "DELETE");
    await call("admin/roles/" + roleId, admin.accessToken, "DELETE");
  }
  if (!keep || process.exitCode) await call("admin/users/" + userId + "/deactivate", admin.accessToken, "POST");
});
