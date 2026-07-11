
      const { z } = require('zod');
      module.exports = { PluginA: class PluginA { constructor() { global.__pluginOrder = global.__pluginOrder || []; global.__pluginOrder.push('A'); } name='A'; version='1.0.0'; tools=[]; handlers={}; } };
    