// ecosystem.config.js — pm2 process manager config
// Usage: pm2 start ecosystem.config.js
// Reload:  pm2 reload spaceos-orchestrator
// Logs:    pm2 logs spaceos-orchestrator --lines 50

module.exports = {
  apps: [
    {
      name: 'spaceos-orchestrator',
      script: 'dist/index.js',
      cwd: '/opt/spaceos/backend/spaceos-orchestrator',
      exec_mode: 'fork',
      instances: 1,
      autorestart: true,
      watch: false,
      max_memory_restart: '512M',
      pmx: false,               // SEC-11: web dashboard disabled
      merge_logs: true,
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      error_file: '/home/gabor/.pm2/logs/orchestrator-error.log',
      out_file:   '/home/gabor/.pm2/logs/orchestrator-out.log',
      env: {
        NODE_ENV: 'development',
        PORT: 3000,
      },
    },
  ],
};
