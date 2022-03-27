// moment and lodash are available to use in your dynamic js script
const moment = require('./moment.min.js')
const _ = require('./lodash.min.js')

// await template and ctx from stdin
let dynamicJsScript = ''
process.stdin.on('data', d => {
  const line = d.toString()
  dynamicJsScript += line

  // Keep collecting input data til c# says it's done sending data
  const messageEnder = /--END-OF-DYNAMIC-JS--$/m
  console.log('|' + line + '|', messageEnder.test(line))
  if (!messageEnder.test(line)) return
  dynamicJsScript = dynamicJsScript.replace(messageEnder, '')

  // Now that we have the full dynamic js, run it and collect its result.
  const runDynamicJs = `(function(ctx) {
    ${dynamicJsScript}
  })()`
  const result = eval(runDynamicJs)

  // Assume that if the dynamic js returns an object or array, they want JSON back
  const resultSerialized = Array.isArray(result) || typeof result === 'object' ? JSON.stringify(result) : result.toString()

  // Surround output with a delimeter, so that you can still console.log in your dynamic js (which also writes to stdout) without messing up c#'s ability to parse out the result of the dynamic js.
  // If you know that your dynamic js won't console.log, this is unnecessary.
  const resultDelimeter = '--RESULT-DELIMITER--'
  process.stdout.write(`${resultDelimeter}\n${resultSerialized}\n${resultDelimeter}`)
  process.exit()
})