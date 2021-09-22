const { execSync } = require('child_process');

try {
  execSync('./run-action.ps1', { stdio: 'inherit', shell: "pwsh" });
} 
catch(err) {
  process.exitcode = 1;
  const msg = err.toString().replace('%', '%25').replace('\r', '%0D').replace('\n', '%0A');
  process.stdout.write("::error::" + msg);
}
