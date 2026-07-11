const { z } = require('zod');

class FailingPlugin {
  constructor(context) {
    throw new Error('Constructor failure');
  }
  get name() { return 'failing'; }
  get version() { return '1.0.0'; }
  get tools() { return []; }
  get handlers() { return {}; }
}

module.exports = { FailingPlugin };
