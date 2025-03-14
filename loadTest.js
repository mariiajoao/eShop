import http from 'k6/http';
import { check, sleep } from 'k6';
import { fail } from 'k6';

export let options = {
    stages: [
        { duration: '30s', target: 10 }, // ramp up to 10 users
        { duration: '1m', target: 10 },  // stay at 10 users for 1 minute
        { duration: '30s', target: 0 },  // ramp down to 0 users
    ],
    insecureSkipTLSVerify: true, // Disable TLS verification
};

export default function () {
    // Navigate to the homepage
    let res = http.get('https://localhost:7298');
    check(res, { 'status was 200': (r) => r.status === 200 }) || fail('Failed to load homepage');


    // Log in as a user
    // ! Replace the URL with the login URL of the application
    let url = 'https://localhost:5243/Account/Login?ReturnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Frequest_uri%3Durn%253Aietf%253Aparams%253Aoauth%253Arequest_uri%253A66D714483C41C7BE9D813CD505C994811D5AAE2365CAF861DAEE5DDED6D96C68%26client_id%3Dwebapp';
    res = http.post(
        url,
        {
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
        }
    );
    check(res, { 'login successful': (r) => r.status === 200 }) || fail('Failed to log in');

    sleep(1);
}
