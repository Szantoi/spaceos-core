
      const { z } = require('zod');
      module.exports = { PluginB: class PluginB { constructor() { global.__pluginOrder = global.__pluginOrder || []; global.__pluginOrder.push('B'); } name='B'; version='1.0.0'; tools=[]; handlers={}; } };
    