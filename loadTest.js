import http from 'k6/http';
import { sleep, check } from 'k6';

// Load test configuration
export const options = {
    // Ramp up pattern: 0->5 users in 10s, stay at 5 users for 30s, then ramp down
    stages: [
        { duration: '10s', target: 5 },  // Ramp up to 5 users
        { duration: '30s', target: 5 },  // Stay at 5 users
        { duration: '10s', target: 0 },  // Ramp down to 0 users
    ],
    thresholds: {
        http_req_failed: ['rate<0.1'],     // Error rate < 10%
        http_req_duration: ['p(95)<1000'], // 95% of requests < 1s
    },
};

export default function () {
    const baseUrl = 'http://localhost:5224';


    const headers = {
        'Content-Type': 'application/json',
        'x-requestid': '12345',
    };

    // Create a basket item
    const basketItems = [
        {
            productId: 1,
            productName: "Test Product",
            unitPrice: 10.99,
            quantity: 2,
        }
    ];

    // Create data with PII information
    const orderData = {
        userId: '1',
        userName: 'User Test',
        city: 'Aveiro',
        street: 'Rua de Aveiro',
        state: 'Av',
        country: 'Portugal',
        zipCode: '123456',
        cardNumber: '4111-1111-1111-1111',
        cardHolderName: 'User Test',
        cardExpiration: new Date(2024, 4, 11),
        cardSecurityNumber: '123',
        cardTypeId: 1,
        buyer: 'test@email.com',
        orderItems: basketItems,
    };

    const order = http.post(
        `${baseUrl}/api/orders?api-version=1.0`,
        JSON.stringify(orderData),
        { headers },
    );

    check(order, {
        'order status is 200': (r) => r.status === 200,
        'order status is 201': (r) => r.status === 201,
    });

    // Log failures for analysis
    if (order.status >= 400) {
        console.log(`Order creation failed: Status ${order.status}, Body: ${order.body.substring(0, 100)}`);
    }

    sleep(1);
}
