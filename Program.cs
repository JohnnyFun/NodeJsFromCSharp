using System;
using Services;
var result = await NodeJsService.RunNodeJsAsync(@"
  return {
    momentWorks: moment('2022-03-27').add(1, 'day').format('YYYY-MM-DD'),
    lodashWorks: _.upperCase('hello'),
  }
");
var expected = "{\"momentWorks\":\"2022-03-28\",\"lodashWorks\":\"HELLO\"}";
Console.WriteLine(result);
if (expected != result) throw new Exception("Expected: " + expected + "\nActual: " + result);