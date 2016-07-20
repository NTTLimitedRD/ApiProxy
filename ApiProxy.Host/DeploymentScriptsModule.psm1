Function Update-ApiProxyConfiguration
{
	Param(
		[ValidateNotNullOrEmpty()]
		[Parameter(Mandatory=$true)]
		[string]$ConfigurationFilePath,		

		[ValidateNotNullOrEmpty()]
		[Parameter(Mandatory = $true)]
		[int] $PortNumber,

		[ValidateNotNullOrEmpty()]
		[Parameter(Mandatory = $true)]
		[String] $CertificateThumbprint,

		[ValidateNotNullOrEmpty()]
		[Parameter(Mandatory = $true)]
		[String] $DefaultApiAddress,
        
		[Parameter(Mandatory = $false)]
		[String] $ApiMocksPath,
	
		[Parameter(Mandatory = $false)]
		[Switch] $RunAsConsole
	)

	$ipPort = "0.0.0.0:$PortNumber"
	$url = "https://+:$PortNumber/"

	Write-Host "Setting up ssl for $ipPort for $url"

	#-- cleanup existing setup --
	netsh http delete sslcert ipport=$ipPort
	netsh http delete urlacl url=$url

	# -- fixup app.config
	$configFile = Join-Path $ConfigurationFilePath "Service/DD.ApiProxy.ServiceHost.exe.config"	
	Write-Host "Updating app config at $configFile" 

	# load the XML from the web.config
	$config = New-Object XML
	$config = [xml](Get-Content $configFile)
		
	$baseAddressUrlConfig = $config.SelectSingleNode("//configuration/appSettings/add[@key=""BaseAddress""]")    
	$baseAddressUrlConfig.Attributes['value'].Value = $url;

	$defaultApiAddressUrlConfig = $config.SelectSingleNode("//configuration/appSettings/add[@key=""DefaultApiAddress""]")    
	$defaultApiAddressUrlConfig.Attributes['value'].Value = $DefaultApiAddress;

	if(-not [string]::IsNullOrEmpty($ApiMocksPath)) {
		$apiMocksPathConfig = $config.SelectSingleNode("//configuration/appSettings/add[@key=""ApiMocksPath""]")    
		$apiMocksPathConfig.Attributes['value'].Value = $ApiMocksPath;
	}
	$config.Save($configFile)

	#-- Add new registration --
	netsh http add urlacl url=$url user=Everyone

	$exeFile = Join-Path $ConfigurationFilePath "Service/DD.ApiProxy.ServiceHost.exe"	

	#AppId is present in the assemblyinfo.cs as Guid
	netsh http add sslcert ipport=$ipPort certhash=$CertificateThumbprint appid='{1CCAA836-44D1-4789-A029-3BA632F4BA37}'

	if($RunAsConsole.IsPresent -eq $true){
		# generally running like this is hard, use octopus or command line
		#& $exeFile  "-r"
		#& $exeFile  "-i" "-u $ServiceAccount" "-p $ServiceAccountPassword"

		& $exeFile
	}
}

export-modulemember -function Update-ApiProxyConfiguration

	
