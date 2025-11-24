I have identified and fixed the issue with the selection feature.

The problem was that while the selection logic was correctly identifying and tracking selected items in the `SelectionManager`, the drawable elements themselves (`Rectangle`, `Ellipse`, etc.) were not visually changing to indicate that they were selected. The `IsSelected` property was being set, but it was not used in their `Draw` methods.

I have now implemented the following changes:
- Updated the `Draw` method in `DrawableRectangle`, `DrawableEllipse`, `DrawableLine`, and `DrawablePath` to render a blue highlight around the shape when `IsSelected` is true.
- Modified `DrawableGroup` to ensure that when a group is selected, the "selected" state is passed down to all of its child elements, so they also appear highlighted.

This provides the necessary visual feedback to the user. The selection feature should now work as expected. The project builds successfully, and you should be able to see the selection highlights when you click on an object with the Select tool.