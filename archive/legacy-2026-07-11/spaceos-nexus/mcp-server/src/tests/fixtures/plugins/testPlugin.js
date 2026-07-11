const { z } = require('zod');

class TestPlugin {
  constructor(context) {
    this.context = context;
    TestPlugin.initCalls = (TestPlugin.initCalls || 0) + 1;
  }

  get name() { return 'test-plugin'; }
  get version() { return '1.0.0'; }
  get tools() {
    return [{ name: 'test_tool', description: 'test', inputSchema: z.any() }];
  }
  get handlers() {
    return {
      test_tool: async (args) => ({ ok: true, args })
    };
  }
  get lifecycle() {
    return {
      onInit: async () => {
        TestPlugin.onInitCalled = true;
      },
      onDestroy: async () => {
        TestPlugin.onDestroyCalled = true;
      }
    };
  }
}

module.exports = { TestPlugin };
