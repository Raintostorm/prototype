# Alpha — part and rig specification

## Layer output required

```text
Alpha_Body.png                 # torso shell; four circular hip sockets visible
Alpha_HeadCannon.png           # head/cannon only, points right
Alpha_FrontLeft_Upper.png      # hip to knee
Alpha_FrontLeft_Lower.png      # knee to foot
Alpha_FrontRight_Upper.png
Alpha_FrontRight_Lower.png
Alpha_RearLeft_Upper.png
Alpha_RearLeft_Lower.png
Alpha_RearRight_Upper.png
Alpha_RearRight_Lower.png
Alpha_HipJoint.png             # optional shared cyan circular joint overlay
Alpha_Shadow.png               # separate flat oval, no rig
```

Every output must be a transparent PNG with margin around the art. A painted checkerboard is invalid.

## Bone hierarchy

```text
root
└── body
    ├── head_cannon
    ├── front_left_hip  ── front_left_knee
    ├── front_right_hip ── front_right_knee
    ├── rear_left_hip   ── rear_left_knee
    └── rear_right_hip  ── rear_right_knee
```

- `root` controls world movement and facing only.
- `body` performs the small weight-shift in idle and walk.
- A leg has two moving bones; its foot remains part of the lower-leg layer.
- Hip-joint artwork is overlaid at each hip and does not need its own bone.
- Shadow changes only scale/alpha during jump or death.

## Animation ownership

| State | Parts that must visibly change |
|---|---|
| idle | body, cannon, all four knees (small offset phase) |
| walk | four hips/knees in alternating pairs; body translates forward |
| atk | body leans, cannon recoils/thrusts, front pair braces |
| hit | body recoils and cannon dips |
| died | body falls; legs fold in sequence; shadow expands then fades |
