import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const orderDuration = new Trend('order_duration', true);
const productDuration = new Trend('product_duration', true);

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 VUs
    { duration: '1m', target: 10 },   // Stay at 10 VUs
    { duration: '30s', target: 50 },  // Ramp up to 50 VUs
    { duration: '2m', target: 50 },   // Stay at 50 VUs
    { duration: '30s', target: 0 },   // Ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    errors: ['rate<0.1'],              // Error rate should be below 10%
  },
};

const BASE_URL = 'http://127.0.0.1:5100';

// Sample order payload
const orderPayload = JSON.stringify({
  storeId: 'store-001',
  firstName: 'K6',
  lastName: 'LoadTest',
  loyaltyId: 'loyal-001',
  orderItems: [
    { productId: 1, quantity: 2 },
    { productId: 2, quantity: 1 },
  ],
});

export default function () {
  // Test 1: GET /Product (retrieve product list)
  const productsResponse = http.get(`${BASE_URL}/Product`);
  check(productsResponse, {
    'GET /Product status is 200': (r) => r.status === 200,
    'GET /Product has products': (r) => JSON.parse(r.body).length > 0,
  }) || errorRate.add(1);
  productDuration.add(productsResponse.timings.duration);

  sleep(0.5);

  // Test 2: POST /Order (create new order)
  const orderResponse = http.post(`${BASE_URL}/Order`, orderPayload, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(orderResponse, {
    'POST /Order status is 200': (r) => r.status === 200,
  }) || errorRate.add(1);
  orderDuration.add(orderResponse.timings.duration);

  sleep(1);

  // Test 3: GET /probes/healthz (health check)
  const healthResponse = http.get(`${BASE_URL}/probes/healthz`);
  check(healthResponse, {
    'GET /probes/healthz status is 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);
}

// Summary report at the end of the test
export function handleSummary(data) {
  return {
    'tests/k6/orderservice-baseline-results.json': JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}

function textSummary(data, options) {
  const indent = options.indent || '';
  const enableColors = options.enableColors || false;

  let summary = '\n';
  summary += `${indent}=== OrderService .NET 6 Baseline Performance Test ===\n\n`;
  summary += `${indent}Test Duration: ${data.state.testRunDurationMs / 1000}s\n`;
  summary += `${indent}VUs: ${data.metrics.vus.values.max}\n`;
  summary += `${indent}Iterations: ${data.metrics.iterations.values.count}\n`;
  summary += `${indent}Requests: ${data.metrics.http_reqs.values.count}\n\n`;

  summary += `${indent}HTTP Request Duration:\n`;
  summary += `${indent}  Min: ${data.metrics.http_req_duration.values.min.toFixed(2)}ms\n`;
  summary += `${indent}  Avg: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms\n`;
  summary += `${indent}  Max: ${data.metrics.http_req_duration.values.max.toFixed(2)}ms\n`;
  summary += `${indent}  P50: ${data.metrics.http_req_duration.values['p(50)'].toFixed(2)}ms\n`;
  summary += `${indent}  P95: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms\n`;
  summary += `${indent}  P99: ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)}ms\n\n`;

  summary += `${indent}Requests Per Second: ${data.metrics.http_reqs.values.rate.toFixed(2)}\n`;
  summary += `${indent}Data Received: ${(data.metrics.data_received.values.count / 1024 / 1024).toFixed(2)} MB\n`;
  summary += `${indent}Data Sent: ${(data.metrics.data_sent.values.count / 1024).toFixed(2)} KB\n\n`;

  if (data.metrics.errors) {
    summary += `${indent}Error Rate: ${(data.metrics.errors.values.rate * 100).toFixed(2)}%\n`;
  }

  summary += `\n${indent}Test completed at: ${new Date().toISOString()}\n`;

  return summary;
}
