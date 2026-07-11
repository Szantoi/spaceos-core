const Database = require('better-sqlite3');
const fs = require('fs');
const path = require('path');
const db = new Database(':memory:');

const episodicMigrationPath = path.join(__dirname,'src/episodic/migrations/003_episodes.sql');
const ftsMigrationPath = path.join(__dirname,'src/episodic/migrations/004_episodes_fts5.sql');

console.log('migrations:', episodicMigrationPath, ftsMigrationPath);

const a = fs.readFileSync(episodicMigrationPath,'utf8');
const b = fs.readFileSync(ftsMigrationPath,'utf8');

// run migrations
 db.exec(a);
 db.exec(b);

 db.exec("INSERT INTO episodes (id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary) VALUES ('ep1','s','d','t','p','[]','[]','Old outcome')");
 console.log('initial fts count', db.prepare('SELECT count(*) as c FROM episodes_fts').get());

 db.prepare("UPDATE episodes SET outcome_summary = ? WHERE id = ?").run('Updated new outcome','ep1');
 console.log('after update fts count', db.prepare('SELECT count(*) as c FROM episodes_fts').get());

 const results = db.prepare("SELECT e.* FROM episodes e INNER JOIN episodes_fts ON episodes_fts.rowid = e.rowid WHERE episodes_fts MATCH ?").all('Updated');
 console.log('search Updated:', results);

 const oldResults = db.prepare("SELECT e.* FROM episodes e INNER JOIN episodes_fts ON episodes_fts.rowid = e.rowid WHERE episodes_fts MATCH ?").all('Old');
 console.log('search Old:', oldResults);
