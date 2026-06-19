/**
 * Middleware Layer - Authentication
 *
 * Token-based authentication for API endpoints
 */

/**
 * Create auth middleware with configuration
 * @param {Object} config - Auth configuration
 * @param {boolean} config.enabled - Whether auth is enabled
 * @param {string} config.token - Valid auth token
 * @returns {Function} Express middleware
 */
export function createAuthMiddleware(config) {
  const { enabled = false, token } = config;

  return (req, res, next) => {
    // Skip auth if disabled
    if (!enabled) {
      return next();
    }

    // Get token from header or query param
    const authHeader = req.headers.authorization;
    const queryToken = req.query.token;

    let providedToken = null;

    if (authHeader) {
      // Support "Bearer <token>" or just "<token>"
      if (authHeader.startsWith('Bearer ')) {
        providedToken = authHeader.slice(7);
      } else {
        providedToken = authHeader;
      }
    } else if (queryToken) {
      providedToken = queryToken;
    }

    // Validate token
    if (!providedToken) {
      return res.status(401).json({
        error: 'Authentication required',
        message: 'Provide token via Authorization header or ?token= query parameter'
      });
    }

    if (providedToken !== token) {
      return res.status(403).json({
        error: 'Invalid token',
        message: 'The provided authentication token is invalid'
      });
    }

    // Token valid, continue
    next();
  };
}

/**
 * Create rate limiting middleware
 * Simple in-memory rate limiter
 * @param {Object} config - Rate limit configuration
 * @param {number} config.windowMs - Time window in milliseconds
 * @param {number} config.maxRequests - Max requests per window
 * @returns {Function} Express middleware
 */
export function createRateLimiter(config) {
  const { windowMs = 60000, maxRequests = 100 } = config;
  const requests = new Map();

  // Cleanup old entries periodically
  setInterval(() => {
    const now = Date.now();
    for (const [ip, data] of requests) {
      if (now - data.windowStart > windowMs) {
        requests.delete(ip);
      }
    }
  }, windowMs);

  return (req, res, next) => {
    const ip = req.ip || req.connection.remoteAddress;
    const now = Date.now();

    if (!requests.has(ip)) {
      requests.set(ip, { count: 1, windowStart: now });
      return next();
    }

    const data = requests.get(ip);

    // Reset window if expired
    if (now - data.windowStart > windowMs) {
      data.count = 1;
      data.windowStart = now;
      return next();
    }

    // Increment and check limit
    data.count++;
    if (data.count > maxRequests) {
      return res.status(429).json({
        error: 'Too many requests',
        message: `Rate limit exceeded. Try again in ${Math.ceil((data.windowStart + windowMs - now) / 1000)} seconds`
      });
    }

    next();
  };
}

export default {
  createAuthMiddleware,
  createRateLimiter
};
