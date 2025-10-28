# Set the range to be *only* the most recent commit (HEAD vs its parent)
$commitRange = "HEAD~1..HEAD"

Write-Host "Analyzing Git objects in most recent commit (range: $commitRange)"

git rev-list --objects $commitRange | `
  git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' | `
  Where-Object { $_ -match "^blob" } | `
  Sort-Object { [int]($_.Split(' ')[2]) } -Descending | `
  Select-Object -First 20