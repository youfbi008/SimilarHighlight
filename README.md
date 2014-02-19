HighlightAndMove
================

HighlightAndMove is a simple extension by the Managed Extensibility Framework.

1.Highlight all similar elements of the selected element.  
2.Change the selection to another int the similar elements by shortcuts.

You can press `Ctrl + Alt + ->` to make the next similar element selected.  
And, you can press `Ctrl + Alt + <-` to make the the previous similar element selected.

# Requirements

* visual studio 2012 sdk

# How to build

1.Build the solution.  
2.When you run this project in the debugger, a second instance of Visual Studio is instantiated.  
3.Set "Command line arguments" of the start options to "/rootsuffix Exp".  

Then, just start running.  
