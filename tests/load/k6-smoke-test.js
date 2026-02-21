// k6 Smoke Test for API Combat Game
// https://apicombat.com
//
// Usage:
//   k6 run k6-smoke-test.js                          # run against production
//   k6 run -e BASE_URL=http://localhost:5000 k6-smoke-test.js  # run locally
//
// Install k6: https://k6.io/docs/get-started/installation/

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const loginDuration = new Trend('login_duration');

// Configuration
const BASE_URL = __ENV.BASE_URL || 'https://apicombat.com';

export const options = {
    stages: [
        { duration: '30s', target: 10 },  // Ramp up to 10 VUs
        { duration: '1m',  target: 10 },  // Sustain 10 VUs
        { duration: '30s', target: 0 },   // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],  // 95% of requests under 500ms
        errors: ['rate<0.01'],             // Error rate under 1%
    },
};

// ── Public endpoints (no auth) ──────────────────────────────────────

function testHealthCheck() {
    const res = http.get(`${BASE_URL}/health`);
    check(res, {
        'health: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

function testVersion() {
    const res = http.get(`${BASE_URL}/api/v1/version`);
    check(res, {
        'version: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

function testSdkStatus() {
    const res = http.get(`${BASE_URL}/api/v1/sdk/status`);
    check(res, {
        'sdk/status: status 200': (r) => r.status === 200,
        'sdk/status: has JSON body': (r) => r.json() !== null,
    }) || errorRate.add(1);
}

function testSdkQuickstart() {
    const res = http.get(`${BASE_URL}/api/v1/sdk/quickstart`);
    check(res, {
        'sdk/quickstart: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

function testSdkEndpoints() {
    const res = http.get(`${BASE_URL}/api/v1/sdk/endpoints`);
    check(res, {
        'sdk/endpoints: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

// ── Auth flow (register → login → authenticated request) ───────────

function testAuthFlow() {
    const uniqueId = `k6_${__VU}_${__ITER}_${Date.now()}`;
    const email = `${uniqueId}@loadtest.local`;
    const password = 'LoadTest123!';

    // Register
    const regRes = http.post(
        `${BASE_URL}/api/v1/auth/register`,
        JSON.stringify({
            username: uniqueId.substring(0, 20),
            email: email,
            password: password,
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );

    const regOk = check(regRes, {
        'register: status 200 or 201': (r) => r.status === 200 || r.status === 201,
    });
    if (!regOk) {
        errorRate.add(1);
        return; // Skip rest if registration failed
    }

    // Login
    const loginStart = Date.now();
    const loginRes = http.post(
        `${BASE_URL}/api/v1/auth/login`,
        JSON.stringify({ email: email, password: password }),
        { headers: { 'Content-Type': 'application/json' } }
    );
    loginDuration.add(Date.now() - loginStart);

    const loginOk = check(loginRes, {
        'login: status 200': (r) => r.status === 200,
        'login: has token': (r) => {
            try { return r.json('token') !== undefined; }
            catch { return false; }
        },
    });
    if (!loginOk) {
        errorRate.add(1);
        return;
    }

    let token;
    try { token = loginRes.json('token'); }
    catch { return; }

    // Authenticated: get roster
    const rosterRes = http.get(`${BASE_URL}/api/v1/player/roster`, {
        headers: { Authorization: `Bearer ${token}` },
    });
    check(rosterRes, {
        'roster: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);

    // Authenticated: get leaderboard
    const lbRes = http.get(`${BASE_URL}/api/v1/leaderboard`, {
        headers: { Authorization: `Bearer ${token}` },
    });
    check(lbRes, {
        'leaderboard: status 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

// ── Main test function ──────────────────────────────────────────────

export default function () {
    testHealthCheck();
    sleep(0.3);

    testVersion();
    sleep(0.3);

    testSdkStatus();
    sleep(0.3);

    testSdkQuickstart();
    sleep(0.3);

    testSdkEndpoints();
    sleep(0.3);

    // Run auth flow less frequently (1 in 5 iterations)
    if (__ITER % 5 === 0) {
        testAuthFlow();
    }

    sleep(1);
}
