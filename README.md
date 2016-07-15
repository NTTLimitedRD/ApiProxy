# ApiProxy

ApiProxy is a proxy and a mock api host.
If it finds a Mock file then it will replay the mock, else will route the request to the default api configured endpoint

How to write Api mocks:
Api Proxy follows a folder and file based paths to the find the mock json file
Typical structure is Folder for the HttpMethod ie GET POST DELETE will have a folder
Then all the url parts except query parameters will have a folder
Special characters are :
1> '_'  for wildcard for a url part
eg : GET  https://apiqa1geo1.itaas.dimensiondata.com/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/infrastructure/datacenter  points to  "\MocksApis\GET\caas\2.3\_\infrastructure\datacenter" with wildcard for orgId 
2> '_q' folder if you wish to create a special handling for query parameters 
eg :GET  https://apiqa1geo1.itaas.dimensiondata.com/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/infrastructure/datacenter?a=b  can points to
            "\MocksApis\GET\caas\2.3\_\infrastructure\datacenter\_q\a=_"
or
            "\MocksApis\GET\caas\2.3\_\infrastructure\datacenter\_q\a=b"
To help create the folder structure, enable RecordingApiToDefaultAddress flag in the config after that any calls going to real api will create appropriate file and folder path in ApiRecordingPath. 
you can edit this file and copy the same structure into mock path to start replaying.
 
File Content :  
1> Replay Mock :
          The important fields are highlighted, this will replay the response with the Status OK 
{
"Method":"GET",
"Uri":"https://xyz/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/consistencyGroup/consistencyGroup/a4f484de-b9ed-43e4-b565-afbf69417615",
"StatusCode":"OK",
"RequestContent":"",
"ResponseContent":"<urn:response requestId=\"na/2014-04-14T13:37:20/62f06368-c3fb-11e3-b29c-001517c4643e\" xmlns:urn=\"urn:didata.com:api:cloud:types\">
<urn:operation>CREATE_CONSISTENCY_GROUP</urn:operation>
<urn:responseCode>RESOURCE_NOT_FOUND</urn:responseCode>
<urn:message>Server 0b9358bb-43d0-4039-b6d5-4580d83d04ea not found.</urn:message>
</urn:response>",
"ResponseContentType":"application/xml",
"Configuration":{"Transform":null,"Mock":true}
}
 
Note: make sure that you escape the " in the request content and response content by \"  in order to not break the JSON
2> Replay Transformed Mock: 
        This is the case supported only for post and put, as we might need to vary the response based on the input content.
Currently varying response status code if done via xsl:message containing content StatusCode=<HttpStatusCode>
{"Method":"GET","UserName":"any","Url":"","StatusCode":"NoContent","RequestContent":"","ResponseContentType":"application/xml","ResponseContent":"<?xml version=\"1.0\" encoding=\"UTF-8\"?><healthcheck>OK Response</healthcheck>","Configuration":{ "Mock":"True", "Transform" : {"RequestContent" :{"XsltFileName":"healthcheck.xslt"}}}}
<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
<xsl:output method="xml" indent="yes"/>
<xsl:template match="name">
<xsl:copy>
<xsl:message terminate="no">StatusCode=OK</xsl:message>
<xsl:element name="greetings">Greetings <xsl:value-of select="./text()" />
</xsl:element>
</xsl:copy>
</xsl:template>
</xsl:stylesheet>
3> Transform Response : 
In this case it will make the real call against the real api  ie https://apiqa1geo1.itaas.dimensiondata.com/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/infrastructure/datacenter
and apply the xslt on top of the response
{
"Method": "GET",
"Uri": "https://xyz/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/infrastructure/datacenter",
"StatusCode": "OK",
"RequestContent": "",
"ResponseContentType": "application/xml",
"Configuration": {"Transform": {"Body": {"XsltFileName": "datacenter.xslt"}}}
}
 
Place datacenter.xslt in the same folder
<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:cloudtypes="urn:didata.com:api:cloud:types"  xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
    <xsl:output method="xml" indent="yes"/> 
  <xsl:template match="cloudtypes:datacenter[@id='QA1_N1_VMWARE_1']">
    <xsl:copy>
      <xsl:copy-of select="@*|node()"/>
       <xsl:element name="cloudtypes:drs">
            <xsl:attribute name="maintenanceStatus">NORMAL</xsl:attribute>
            <xsl:element name="cloudtypes:targetDatacenters">QA1_N2_VMWARE_1 QA1_N2P_VMWARE_2</xsl:element>
        </xsl:element>           
    </xsl:copy>
  </xsl:template>
   <xsl:template match="cloudtypes:datacenter[@id='QA1_N2_VMWARE_1']">
    <xsl:copy>
      <xsl:copy-of select="@*|node()"/>
      <xsl:element name="cloudtypes:drs">
            <xsl:attribute name="maintenanceStatus">NORMAL</xsl:attribute>
            <xsl:element name="cloudtypes:targetDatacenters">QA1_N2P_VMWARE_2</xsl:element>
        </xsl:element>     
    </xsl:copy>
  </xsl:template>
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
4> Transform Request :
      a. Trimming Query parameters : this will help remove any query parameter to be sent to the real api server
eg:
{"Method":"GET","Uri":"https://api-au.dimensiondata.com/caas/2.3/a4f484de-b9ed-43e4-b565-afbf69417615/server/server","StatusCode":"OK","RequestContent":"","ResponseContentType":"application/xml","Configuration":{"Transform" : {"Body" :{"XsltFileName":"serverdrsfilter.xslt"}, "Query":{"TrimQueryParameters":"drsEligible"}}}}
 
     b. Request content transform:
         Currently it support request contents of type application/xml and  application/x-www-form-urlencoded
 
i.application/x-www-form-urlencoded
 
{"Method":"POST","Uri":"","StatusCode":"OK","RequestContent":"","ResponseContent":"","ResponseContentType":"application/xml","Configuration":{"Transform":{"RequestContent":{"TrimQueryParameters":"role=drs"}},"Mock":false}}
ii.application/xml (xslt based)
{"Method":"POST","Uri":"https://apiqa1geo1.itaas.dimensiondata.com/oec/0.9/5c2255c0-5d81-44d4-8a23-fe0f8605be9b/accountWithPhoneNumber/","StatusCode":"OK","RequestContent":"","ResponseContentType":"application/xml","Configuration":{"Transform" : {"RequestContent" :{"XsltFileName":"userAccount.xslt"}}}}
 
 
