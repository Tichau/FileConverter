param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$method=$assembly.GetType('FileConverter.PathHelpers').GetMethod('GenerateFilePathFromTemplate')
$pdf=[Enum]::Parse($assembly.GetType('FileConverter.OutputType'),'Pdf')
$script:count=0
$script:failures=0
function Check($label,$inputPath,$template,$expected) {
 $actual=$method.Invoke($null,@($inputPath,$pdf,$template,3,12))
 $script:count++
 if($actual -cne $expected) { $script:failures++; "FAIL $label`n  expected: $expected`n  actual:   $actual" }
 else { "PASS $label => $actual" }
}
Check 'issue797' 'C:\work\99 (O) my file.jpg' '(p)(f)' 'C:\work\99 (O) my file.pdf'
foreach($token in '(O)','(o)','(i)','(I)','(d0)','(D0)','(filename)','(f)','(F)') {
 Check "filename literal $token" "C:\work\99 $token my file.jpg" '(p)(f)' "C:\work\99 $token my file.pdf"
 Check "folder literal $token" "C:\work $token\plain.jpg" '(p)(f)' "C:\work $token\plain.pdf"
}
Check 'explicit tokens and aliases' 'C:\work\photo.JPG' '(path)(filename)-(outputext)-(o)-(O)-(inputext)-(i)-(I)-(n:i)-(n:c)' 'C:\work\photo-pdf-pdf-PDF-JPG-JPG-JPG-3-12.pdf'
Check 'uppercase filename preserves tokens' 'C:\work\my (o) file.jpg' '(p)(F)' 'C:\work\MY (O) FILE.pdf'
Check 'directory indices' 'C:\first\second\photo.jpg' '(d2)\(d1)\(D0)\(f)' 'C:\first\SECOND\photo.pdf'
Check 'inserted directory stays literal' 'C:\first\(O)\photo.jpg' '(d0)\(f)' '(O)\photo.pdf'
Check 'unknown and out of range tokens' 'C:\work\photo.jpg' '(unknown)-(d99)-(d00)-(N:i)-(f)' '(unknown)-(d99)-(d00)-(N:i)-photo.pdf'
Check 'repeated tokens' 'C:\work\(O).jpg' '(f)-(f)-(O)' '(O)-(O)-PDF.pdf'
Check 'empty default template' 'C:\work\(O).jpg' '' 'C:\work\(O).pdf'
Check 'empty input' '' '(p)(f)' 'Invalid input file path (argument 0).'
Check 'date in template' 'C:\work\photo.jpg' '(d:yyyy)-(f)' "$([DateTime]::Now.ToString('yyyy'))-photo.pdf"
Check 'date format output is literal' 'C:\work\photo.jpg' "(d:'(O')" '(O.pdf'
foreach($pair in @(@('d','documents','MyDocuments'),@('m','music','MyMusic'),@('v','videos','MyVideos'),@('p','pictures','MyPictures'))) {
 $folder=[Environment]::GetFolderPath([Environment+SpecialFolder]::$($pair[2]))+'\'
 Check "special folder $($pair[1])" 'C:\work\photo.jpg' "(p:$($pair[0]))(f)" ($folder+'photo.pdf')
 Check "special folder alias $($pair[1])" 'C:\work\photo.jpg' "(p:$($pair[1]))(f)" ($folder+'photo.pdf')
}
"RESULT: $count checks; $failures failures"
if($failures) { exit 1 }
