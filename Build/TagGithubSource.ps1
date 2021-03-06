Param 
(
    [Parameter(Mandatory=$True)] [string]$Tag,
    [Parameter(Mandatory=$True)] [string]$Commit,
    [Parameter(Mandatory=$True)] [string]$AuthToken,
    [Parameter(Mandatory=$False)] [string]$Repo = "hybridportbridge",
    [Parameter(Mandatory=$False)] [string]$Owner = "alexey-kadyrov"
)

# See http://blog.travisgosselin.com/vsts-build-task-github-tag/

Write-Host "Adding GIT tag $Tag for repo $Repo under owner $Owner"

$resource = "https://api.github.com/repos/$Owner/$Repo/git/refs"

Write-Host "Posting to URL: $resource"

$body = @{
    ‘ref’ = "refs/tags/$Tag";
    ‘sha’ = "$Commit"}

$jsonBody = (ConvertTo-Json $body)
Write-Host "Posting with Body: $jsonBody"

$result = Invoke-RestMethod -Method Post -Uri $resource -Body (ConvertTo-Json $body) -Header @{"Authorization" = "token $AuthToken"}

Write-Host $result