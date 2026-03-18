# Simple CSS Minifier Script
# Reduces file size by removing unnecessary whitespace, comments, and optimizing formatting

param(
    [string]$InputFile = "..\src\SalesCobrosGeo.Web\wwwroot\css\site.css",
    [string]$OutputFile = "..\src\SalesCobrosGeo.Web\wwwroot\css\site.min.css"
)

Write-Host "CSS Minification Tool" -ForegroundColor Cyan
Write-Host "=====================`n" -ForegroundColor Cyan

# Read the CSS file
$css = Get-Content -Path $InputFile -Raw -Encoding UTF8
$originalSize = $css.Length

Write-Host "Original file: $InputFile" -ForegroundColor Gray
Write-Host "Original size: $($originalSize / 1KB) KB`n" -ForegroundColor Gray

# Remove CSS comments (/* ... */)
$css = $css -replace '/\*[\s\S]*?\*/', ''

# Remove leading and trailing whitespace from lines
$css = $css -replace '^\s+', '' -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
$css = $css -join "`n"

# Remove whitespace around { } : ; , operators
$css = $css -replace '\s*{\s*', '{'
$css = $css -replace '\s*}\s*', '}'
$css = $css -replace '\s*:\s*', ':'
$css = $css -replace '\s*;\s*', ';'
$css = $css -replace '\s*,\s*', ','
$css = $css -replace ';\s*}', '}'

# Remove multiple consecutive spaces
$css = $css -replace '\s{2,}', ' '

# Remove spaces around > + ~ operators in selectors
$css = $css -replace '\s*>\s*', '>'
$css = $css -replace '\s*\+\s*', '+'
$css = $css -replace '\s*~\s*', '~'

# Optimize color values (e.g., #ffffff -> #fff)
$css = $css -replace '#([0-9a-fA-F])\1([0-9a-fA-F])\2([0-9a-fA-F])\3\b', '#$1$2$3'

# Remove unnecessary zeros (e.g., 0.5 -> .5, 0px -> 0)
$css = $css -replace '\b0+\.(\d+)', '.$1'
$css = $css -replace '\b(\d+)\.0+([^\d])', '$1$2'
$css = $css -replace '\b0(px|em|%|rem|vh|vw|vmin|vmax|cm|mm|in|pt|pc)\b', '0'

# Remove space after : in URLs
$css = $css -replace 'url\(\s*', 'url('
$css = $css -replace '\s*\)', ')'

# Compact newlines (keep some structure for better debugging)
$css = $css -replace '\n{2,}', "`n"

# Write the minified CSS
$minifiedSize = $css.Length
$reduction = [math]::Round((($originalSize - $minifiedSize) / $originalSize) * 100, 2)

Set-Content -Path $OutputFile -Value $css -Encoding UTF8 -NoNewline

Write-Host "Minified file: $OutputFile" -ForegroundColor Green
Write-Host "Minified size: $($minifiedSize / 1KB) KB" -ForegroundColor Green
Write-Host "Size reduction: $reduction%" -ForegroundColor Green
Write-Host "`nMinification completed successfully!" -ForegroundColor Cyan
