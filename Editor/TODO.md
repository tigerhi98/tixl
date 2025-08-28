## Important issues

- [?] Connections from input are sometimes not correctly evaluated 
- [ ] Rearranging parameters with additional annotations (e.g. ShaderParameters) breaks operator 
- [ ] Pre/Post Curve modes are applied to all (not just selected curves)
- [ ] Indicate Pre/Post curve moves in timeline

- [ ] Ask before removing inputs and outputs (can't be undone)
- [ ] Fix MultiInput connection editing
- [x] Indicated HDR colors
- [ ] Combine into new Symbol should prefill current project and namespace
- [ ] Command bar shortcuts should work if UI is hidden
- [ ] Inserting keyframes does not always use neighbour interpolation type
- [x] Fix fade graph when background is interactive 
- [ ] Looks like only last animated value edit to a vec3 can't be undone?
- [x] Remove clear background image when clicking on left edge
- [ ] Maybe bookmarks should toggle pinning?
- [ ] Rethink bookmarks -> Add marker in Op with number / switch with numbers only. only bring to view if hidden
- [ ] Export should use project folder and some prefix like _

next:
- [x] Don't show hidden input popup on snap


## Todo / Meetup 2025-08-25:

- [ ] RadialPoints should have Color parameter
- [ ] Improve RadialPoint parameter pair documentation
- [x] AnimInt should have OpUi and Modulo parameter
- [ ] Check if [ForwardBeatTap] is still working
- [x] AnimBool visualization should look less scary if not evaluated yet
- [x] Fix DrawPointsShaded fadeOutToo close parameter
- [x] Try to improve searching with Ctrl+F focus into view behaviour
- [ ] fix clamping of rounded numbers in infinity slider
- [x] Connected parameters names should be renamable
- [ ] Check Gradient interpolation override caching with LinearGradient


# UI

## Feedback from UncleX

- [ ] Graph context menu should have an option to Add (and show keyboard shortcut)
- [ ] Maybe add option to insert ops via Esc
- [x] Fix "don't disconnect unsnap"
- [x] Drag and split vertical connection lines
- [x] Output-Nodes should have a show in Output indicator
- [ ] Tooltip + short for pinning in Output window bar
- [ ] Create [HowToUseVariables]

## Project handling / Project HUB

- [ ] Project settings should save output resolution
- [ ] Project hub context menu Open in Explore is not working #719
- [ ] Load last project from user settings
- [x] Scrolling in project hub list #716
- [ ] unload projects from project list
- [ ] Project backups should be project specific
- 

## Graph

- [ ] Publish as input does not create connection
- 
- [ ] Split Connections on drop
- [x] Rewiring of vertical connection lines
- [ ] LoadImage has no thumbnail
- [ ] Panning/Zooming in CurveEdit-Popup opened from SampleCurveOp is broken 
- [ ] Create connections from dragging out of parameter window
- [ ] Add hint message to hold shift for keeping connections
- [x] Raymarch UV spaces are not working
- [x] FractalSDF -> SDFToColor -> FieldToImage (with color mode) is not working
- [x] RandomizePoints HSB broken
- [x] Add Field support for DrawMeshAtPoints 
- [x] Ui Tweaks: Hide TabGroup close
- [ ] Refactor IStatusMessageProvider "Success" indication #714

## Timeline

- [x] Implement delete clips
- [x] Soundtrack image is incorrectly scaled with playback?
- [x] After deleting and restart recompilation of image is triggered, but image in timeline is not updated?
      Path not found: '/pixtur.still.Gheo/soundtrack/DARKrebooted-v1.0.mp3' (Resolved to '').
- [x] Allow Dragging up/down with right mouse-button
- [x] Add option to squeeze Layers area 

## UI-Scaling Issues (at x1.5):

- [x] Perlin-Noise graph cut off
- [x] Timeline-Clips too narrow
- [ ] Full-Screen cuts of timeline ruler units
- [ ] MagGraph-Labels too small
- [ ] Panning Canvas offset is scaled
- [ ] Pressing F12 twice does not restore the layout
- [ ] Snapping is too fine
- [ ] in Duplicate Symbol description field is too small

- [ ] Add some kind of FIT button to show all or selected operators 

## High frame-rate issues 120Hz
- [x] Shake doesn't work with 120hz

## Ops

- [x] Remove Time 2nd output
- [x] Rename Time2 <-> Time
- [ ] Rounded Rect should have blend parameter
- [x] Fix BoxGradient
- [x] SetEnvironment should automatically insert textureToCubemap
- [ ] Remove Symbol from Editor
- [ ] Fix SnapToPoints
- [ ] Sort out obsolete pixtur examples
- [?] Rename PlayVideo to LoadVideo
- [ ] Add [OrientImage] with flip, rotate 90d, 180d 270d
- [ ] Clean up [SnapPointsToGrid] with amount
- [ ] FIX: Filter returns a point with count 0 (with random-seed not applied)
- [ ] Deprecate DrawPoints2
- [x] Fix [RandomizePointsColor] !
- [ ] Cleanup *-template.hlsl -> -gs.hlsl
- [ ] [Set-] and [BlendSnapshots] (see API mock examples)
    
### Particles
- [ ] Provide optional reference to points in [GetParticleComponents]
- 
## SDF-Stuff

- [ ] Changing the parameter order in the parameter window will break inputs with [GraphParam] attribute
- [ ] Ray marching glow
- [ ] Some form of parameter freezing
- [x] Combine flood fill with 3d
- [x] FieldToImage
- [ ] Flexible shader injection (e.g. DrawMesh normals, etc.)
- [ ] ShaderGraphNode should be bypassable
- [ ] Undo/Redo seems to be broken when editing custom SDF shaders

## Documentation

- [x] Fix WIKI export does not include input descriptions

## General UX-ideas:

- [x] Add mono-space font for code fragments
- [ ] StatusProvideIcon should support non-warning indicator
- [ ] Separate Value Clamping for lower and upper values 
- [ ] Drag and drop of files (copy them to resources folder and create LoadXYZ instance...)
- [ ] With Tapping and Beat-Lock, no Idle-Animation should probably "pause" all playback?
 
## Other features

- [ ] EXR image sequence support #740

## Refactoring
- [ ] Remove ICanvas
- [ ] Refactor to use Scopes

## Long-Term ideas:
- [ ] Render-Settings should be a connection type, including texture sampling, culling, z-depth