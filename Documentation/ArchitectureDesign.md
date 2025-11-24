## **Software Requirements Specification: Child-Centric Drawing Application**

This document outlines the essential features and constraints necessary for developing a free, ad-free drawing application targeting children aged 3–8.

## **Architecture & Design**

The overall structure of the project follows best practices using Reactive UI (http://reactiveui.net)
The application is centered around the SkiaSharp (https://learn.microsoft.com/en-us/dotnet/api/skiasharp) library for vector graphics.
Utilizing messages via MessageBus with limited use. Only messages that need to be broadcast to disconnected (low-coupled) components. Another approach would be to utilize reactive methodologies and failing that a command/event pattern.
