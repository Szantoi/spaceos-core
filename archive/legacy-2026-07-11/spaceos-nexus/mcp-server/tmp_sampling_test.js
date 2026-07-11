const { SamplingService } = require('./src/mcp/sampling/SamplingService');

(async () => {
    const s = new SamplingService(5000);
    const p = s.requestSampling('sess', { prompt: 'p', options: [{ label: 'a', value: 'a' }], timeoutMs: 4000 });
    const list = s.listPending('sess');
    console.log('pending', list);
    const id = list[0]?.requestId;
    console.log('id', id, 'resolve', s.resolveSampling(id, ['a']));
    console.log('result', await p);
})();
