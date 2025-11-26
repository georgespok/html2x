# Quickstart: Independent Borders

This feature enables standard CSS border rendering. No special configuration is required; it works out-of-the-box with standard HTML/CSS.

## Usage

Write HTML with specific border properties:

```html
<div style="
    border-top: 5px solid red;
    border-right: 2px dashed blue;
    border-bottom: 10px solid green;
    border-left: 1px dotted black;
">
    Content with independent borders.
</div>
```

## Verification

Use the Test Console to verify the output:

```powershell
# 1. Create the sample file
Set-Content src/Tests/Html2x.TestConsole/html/independent-borders.html @"
<!DOCTYPE html>
<html>
<head>
    <style>
        .demo-box {
            width: 200px;
            height: 100px;
            background-color: #f0f0f0;
            border-top: 5px solid #ff0000;
            border-right: 10px solid #00ff00;
            border-bottom: 15px solid #0000ff;
            border-left: 20px solid #ffff00;
            margin: 20px;
        }
    </style>
</head>
<body>
    <h1>Independent Borders Test</h1>
    <div class="demo-box"></div>
</body>
</html>
"@

# 2. Run the converter
dotnet run --project src/Tests/Html2x.TestConsole/Html2x.TestConsole.csproj -- --input src/Tests/Html2x.TestConsole/html/independent-borders.html --output build/output.pdf
```

## What to Look For
- **Four Colors**: Each side has a distinct color.
- **Variable Widths**: Sides have different thicknesses.
- **Corners**: Corners will show a simple rectangular overlap where borders meet.