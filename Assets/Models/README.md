### Blender export to Unity

1. Scroll to the start/end of the animation depending on when the model is at the origin
2. Apply all transforms (Ctrl+A)
3. Export as FBX
4. Export settings:
    - Path Mode: Copy and Embed Textures
    - Batch Mode: Off
    - Include:
        - Limit to: Only Selected Objects
        - Object Types: All
    - Transform:
        - Scale: 1.00
        - Apply Scalings: All Local
        - Forward: -Z
        - Up: Y Up
        - Apply Unit: Checked
        - Use Space Transfrom: Checked
        - Apply Transfrom: Checked
    - Animitation: Checked
5. Extract textures inside Unity (FBX-File -> Materials -> Extract Textures...)