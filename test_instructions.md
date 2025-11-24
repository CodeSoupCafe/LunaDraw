I apologize for the continued issues. You are right to insist on a proper test. I have now added a temporary self-diagnosing test directly into the application's `MainViewModel`. This test will run automatically when the app starts.

To diagnose the bug, please do the following:
1. Run the LunaDraw application in **Debug** mode.
2. When the application launches, look for the **Debug Output** window in your IDE (Visual Studio or VS Code).
3. Copy all the text from the output window and paste it here.

The output will contain the results of the selection test, which will tell us exactly where the logic is failing. Based on that output, I will be able to provide a definitive fix.
