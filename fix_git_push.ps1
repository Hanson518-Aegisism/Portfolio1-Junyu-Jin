Set-Location -LiteralPath "D:\unityproject\Portfolio1 Junyu Jin"
$log = @()

function Log($msg) { $script:log += $msg; Write-Host $msg }

Log "=== git status ==="
Log (git status -sb 2>&1 | Out-String)

$urp = "Assets/Mechanical_Damage_VFX/HDRP_and_URP_Pipelines/Mechanical_Damage_VFX_URP.unitypackage"
$hdrp = "Assets/Mechanical_Damage_VFX/HDRP_and_URP_Pipelines/Mechanical_Damage_VFX_HDRP.unitypackage"

foreach ($f in @($urp, $hdrp)) {
    if (git ls-files --error-unmatch $f 2>$null) {
        git rm --cached -f $f 2>&1 | ForEach-Object { Log $_ }
        Log "Removed from index: $f"
    } else {
        Log "Not tracked: $f"
    }
}

git add .gitignore 2>&1 | ForEach-Object { Log $_ }
git commit --amend -m "Initial commit: Unity project (exclude large unitypackages)" 2>&1 | ForEach-Object { Log $_ }

Log "=== tracked unitypackages ==="
Log (git ls-files "*unitypackage*" 2>&1 | Out-String)

Log "=== last commit ==="
Log (git log -1 --oneline 2>&1 | Out-String)

Log "=== large blobs in HEAD (>100MB) ==="
git rev-list --objects HEAD | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' |
    Where-Object { $_ -match '^blob ' } |
    ForEach-Object {
        if ($_ -match '^blob \S+ (\d+) (.*)$') {
            $size = [int64]$matches[1]
            if ($size -gt 104857600) {
                Log ("{0:N2} MB  {1}" -f ($size/1MB), $matches[2])
            }
        }
    }

$log | Out-File -FilePath "D:\unityproject\Portfolio1 Junyu Jin\fix_git_push_result.txt" -Encoding utf8
