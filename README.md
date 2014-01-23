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
When the cursor between the two WriteLine method,  
the string "abcde" and "hijkl" will be highlighted.
Then you press `Ctrl + Alt + <-`, then the whole text of "abcde" will be selected.

