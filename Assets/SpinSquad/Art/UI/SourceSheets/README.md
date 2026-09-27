# Spin Squad — missing button source sheet

Use `SpinSquad_MissingButtons_States_Transparent.png` as the slicing source. It is an RGBA image with a genuinely transparent background. The similarly named `_Source` file is retained only as the untouched AI source and must not be imported into the game.

Each button is shown in this order: **normal**, **pressed**, **disabled**.

Export the slices as PNG with these exact base names under `Assets/SpinSquad/Resources/UI/ButtonKit/`:

- `start`, `start_pressed`, `start_disabled`
- `roll`, `roll_pressed`, `roll_disabled`
- `add_ally`, `add_ally_pressed`, `add_ally_disabled`
- `bag_closed`, `bag_closed_pressed`, `bag_closed_disabled`
- `bag_open`, `bag_open_pressed`, `bag_open_disabled`
- `merge`, `merge_pressed`, `merge_disabled`
- `sell`, `sell_pressed`, `sell_disabled`
- `info`, `info_pressed`, `info_disabled`
- `close`, `close_pressed`, `close_disabled`
- `resume`, `resume_pressed`, `resume_disabled`
- `restart`, `restart_pressed`, `restart_disabled`

Keep each exported canvas tight to the outer dark outline, with 2–4 transparent pixels of padding. Do not bake a checkerboard or colored matte into exported PNG files.
