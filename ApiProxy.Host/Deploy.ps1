Import-Module  (Join-Path $PSScriptRoot "DeploymentScriptsModule.psm1") -DisableNameChecking

Update-ApiProxyConfiguration -ConfigurationFilePath  (Join-Path $PSScriptRoot '.') `
	-PortNumber  $OctopusParameters["MockApi_Port"]  `
	-CertificateThumbprint  $OctopusParameters["MockApi_CertificateThumbprint"]  `
	-DefaultApiAddress $OctopusParameters["MockApi_DefaultApiAddress"]  `	
