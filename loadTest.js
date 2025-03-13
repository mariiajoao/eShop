import http from 'k6/http';
import { check, sleep } from 'k6';
import { fail } from 'k6';

export let options = {
  stages: [
    { duration: '10s', target: 1 },
    // { duration: '30s', target: 10 }, // ramp up to 10 users
    // { duration: '1m', target: 10 },  // stay at 10 users for 1 minute
    // { duration: '30s', target: 0 },  // ramp down to 0 users
  ],
  insecureSkipTLSVerify: true, // Disable TLS verification
};

export default function () {
  // Navigate to the homepage
  let res = http.get('https://localhost:7298');
  check(res, { 'status was 200': (r) => r.status === 200 }) || fail('Failed to load homepage');


  // // Get the antiforgery token
  // // Get the antiforgery token
  // res = http.get('https://localhost:7298/api/antiforgery/token');
  // console.log('Antiforgery token response status:', res.status);
  // console.log('Antiforgery token response body:', res.body);
  // check(res, { 'status was 200': (r) => r.status === 200 }) || fail('Failed to get antiforgery token');
  // const token = res.json('token');


  // Log in as a user
  res = http.post(
    'https://localhost:5243/Account/Login?ReturnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Frequest_uri%3Durn%253Aietf%253Aparams%253Aoauth%253Arequest_uri%253AA427902FE727DF5C301EFD5AB009D22DA1ACD6E27AA0EA28435AD5C42A3C22A9%26client_id%3Dwebapp',
    {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      }
    }
  );
  console.log('Login response body:', res.body);
  check(res, { 'login successful': (r) => r.status === 200 }) || fail('Failed to log in');

  // const antiforgery = 'CfDJ8B919G8OIJFIuhEn7zsKHEB9dNjjyA4MPz8nYnOZXsS0Q7gk6nvI2nK7GPjBhWoLd7moXpGYNhFgALv-Dkfc79hvBpFjnC8y0IXy_LmAyGEh3RCJcbwk7BHsmAif9TeB4DAVwb-4HN-vWEbb_ncG4LY';

  // // // Add an item to the basket
  // res = http.post(
  //   'https://localhost:7298/item/99',
  //   {
  //     headers: {
  //       'Content-Type': 'application/json',
  //       '.Aspire.Dashboard.Antiforgery.': antiforgery.json
  //     }
  //   }
  // );
  // console.log('Item to basket response body:', res.body);
  // check(res, { 'item added to basket': (r) => r.status === 200 }) || fail('Failed to add item to basket');

  // // Go to the basket
  // res = http.get('https://localhost:7298/api/basket');
  // check(res, { 'basket page loaded': (r) => r.status === 200 }) || fail('Failed to load basket page');

  // // Proceed to checkout
  // res = http.post('https://localhost:7298/api/checkout', JSON.stringify({
  //   address: '123 Test St',
  //   city: 'Test City',
  //   postalCode: '12345',
  //   country: 'Test Country',
  // }), { headers: { 'Content-Type': 'application/json' } });
  // check(res, { 'checkout successful': (r) => r.status === 200 }) || fail('Failed to checkout');

  // // Submit the order
  // res = http.post('https://localhost:7298/api/orders');
  // check(res, { 'order placed': (r) => r.status === 200 }) || fail('Failed to place order');

  // // Verify the order confirmation
  // res = http.get('https://localhost:7298/api/orders/confirmation');
  // check(res, { 'order confirmation received': (r) => r.status === 200 }) || fail('Failed to receive order confirmation');

  sleep(1);
}