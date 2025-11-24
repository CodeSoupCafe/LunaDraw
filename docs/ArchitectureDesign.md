## **Software Requirements Specification: Child-Centric Drawing Application**

This document outlines the essential features and constraints necessary for developing a free, ad-free drawing application targeting children aged 3–8.

## **Architecture & Design**

The application is centered around the SkiaSharp (https://learn.microsoft.com/en-us/dotnet/api/skiasharp) library for vector graphics.
The aim is to reduce the complexity of the application as greatly as possible by incorporating ReactiveUI (reactiveui.net).
Utilizing messages via MessageBus with limited use. Only messages that need to be broadcast to disconnected (low-coupled) components. Another approach would be to utilize reactive methodologies and failing that a command/event pattern.
