/**
 * src/routes/proxy.route.ts
 *
 * Proxy routes for Joinery, Identity, and Cutting module APIs
 * Routes FE requests to backend services
 */

import express, { Request, Response } from 'express';
import axios, { AxiosError } from 'axios';
import { env } from '../config/env';

export const proxyRouter = express.Router();

// Service URLs from env.ts (validated at startup)
const JOINERY_BASE_URL = env.JOINERY_BASE_URL;
const IDENTITY_BASE_URL = env.IDENTITY_BASE_URL;
const CUTTING_BASE_URL = env.CUTTING_BASE_URL;

/**
 * Joinery: POST /api/products/configure
 * Configurator endpoint — forwards to Joinery service
 */
proxyRouter.post('/products/configure', async (req: Request, res: Response) => {
  try {
    const response = await axios.post(
      `${JOINERY_BASE_URL}/joinery/api/products/configure`,
      req.body,
      {
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Joinery service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Joinery: POST /api/work-orders
 * Work order creation endpoint — forwards to Joinery service
 */
proxyRouter.post('/work-orders', async (req: Request, res: Response) => {
  try {
    const response = await axios.post(
      `${JOINERY_BASE_URL}/joinery/api/work-orders`,
      req.body,
      {
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Joinery service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Joinery: GET /api/work-orders/:id/sheet.pdf
 * Work order PDF sheet endpoint — forwards to Joinery service
 */
proxyRouter.get('/work-orders/:id/sheet.pdf', async (req: Request, res: Response) => {
  try {
    const { id } = req.params;
    const response = await axios.get(
      `${JOINERY_BASE_URL}/joinery/api/work-orders/${id}/sheet.pdf`,
      {
        timeout: 5000,
        headers: {
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
        responseType: 'arraybuffer',
      }
    );
    res.status(response.status);
    if (response.headers['content-type']) {
      res.set('Content-Type', response.headers['content-type']);
    }
    res.send(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Joinery service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Joinery: GET /api/orders/:id/material-req
 * Forwards to Joinery service
 */
proxyRouter.get('/orders/:id/material-req', async (req: Request, res: Response) => {
  try {
    const { id } = req.params;
    const response = await axios.get(
      `${JOINERY_BASE_URL}/api/orders/${id}/material-req`,
      {
        timeout: 5000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true, // Accept any status code (don't throw on 4xx/5xx)
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    // Only network errors (ECONNREFUSED, timeout, etc.) reach here
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Joinery service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Joinery: GET /api/orders/:id/hardware-list
 * Forwards to Joinery service
 */
proxyRouter.get('/orders/:id/hardware-list', async (req: Request, res: Response) => {
  try {
    const { id } = req.params;
    const response = await axios.get(
      `${JOINERY_BASE_URL}/api/orders/${id}/hardware-list`,
      {
        timeout: 5000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Joinery service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Cutting: POST /api/cutting/plans
 * Generates daily cutting plan
 */
proxyRouter.post('/cutting/plans', async (req: Request, res: Response) => {
  try {
    const response = await axios.post(
      `${CUTTING_BASE_URL}/api/cutting/plans`,
      req.body,
      {
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Cutting service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Cutting: GET /api/cutting/plans
 * Retrieves cutting plans for a specific date
 */
proxyRouter.get('/cutting/plans', async (req: Request, res: Response) => {
  try {
    const { date } = req.query;
    let url = `${CUTTING_BASE_URL}/api/cutting/plans`;
    if (date) {
      url += `?date=${date}`;
    }

    const response = await axios.get(url, {
      timeout: 5000,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': req.headers['authorization'] || '',
      },
      validateStatus: () => true,
    });
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Cutting service unavailable' };
    res.status(status).json(data);
  }
});

/**
 * Identity proxy router (separate mount point: /identity)
 */
export const identityProxyRouter = express.Router();

/**
 * Identity: GET /identity/users
 * Forwards to Identity service
 */
identityProxyRouter.get('/users', async (req: Request, res: Response) => {
  try {
    const response = await axios.get(
      `${IDENTITY_BASE_URL}/identity/users`,
      {
        timeout: 5000,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': req.headers['authorization'] || '',
        },
        validateStatus: () => true,
      }
    );
    res.status(response.status).json(response.data);
  } catch (err) {
    const axiosErr = err as AxiosError;
    const status = axiosErr.response?.status || 502;
    const data = axiosErr.response?.data || { error: 'Identity service unavailable' };
    res.status(status).json(data);
  }
});

export default proxyRouter;
