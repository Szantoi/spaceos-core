const { z } = require('zod');

class OnInitFailPlugin {
  constructor(context) {
    // No-op constructor; allow instantiation
  }

  get name() { return 'onInitFail'; }
  get version() { return '1.0.0'; }
  get tools() { return []; }
  get handlers() { return {}; }

  get lifecycle() {
    return {
      async onInit() {
        throw new Error('onInit failed');
      },
      async onError(err) {
        global.__pluginErrorHandled = global.__pluginErrorHandled || [];
        global.__pluginErrorHandled.push(err.message);
      }
    };
  }
}

module.exports = { OnInitFailPlugin };
