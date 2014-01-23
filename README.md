HighlightAndMove
================

HighlightAndMove is a simple extension by the Managed Extensibility Framework.

1.Highlight all output text of "WriteLine" method when the cursor under the "WriteLine" method.  
2.Let you conveniently select output text of "WriteLine" method by shortcuts.

You can press `Ctrl + Alt + ->` to select the output text of the next "WriteLine" method.  
And, you can press `Ctrl + Alt + <-` to select the output text of the previous "WriteLine" method.

For example,
```C#
Console.WriteLine("abcde");
DoSomething();
Debug.WriteLine("hijkl");
```
When the cursor is moved between the two WriteLine method,  
the string "abcde" and "hijkl" will be highlighted.
Then you press `Ctrl + Alt + <-`, then the whole text of "abcde" will be selected.

# How to build

1.Build the solution.
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.
3.Set "Command line arguments of the start options to "/rootsuffix Exp".

Then, just start running.
