# Check TLS for Sites

`chktls` is a simple utility to show certificate information for a given web
site helping you to debug certificate issues you might have.

Usage:
```
chktls <url>
```

## Example
```
$ chktls https://example.com
GET https://example.com
200 (OK)
Age: 397410
Cache-Control: max-age=604800
Date: Thu, 17 Feb 2022 16:24:09 GMT
[...]
_______________________________________________________________________________________________________________________
Certificate chain:
  [0]         Version: 3
         SerialNumber: 3084688434162232513624747060761048124
             IssuerDN: C=US,O=DigiCert Inc,CN=DigiCert TLS RSA SHA256 2020 CA1
           Start Date: 10/12/2021 00:00:00
           Final Date: 09/12/2022 23:59:59
            SubjectDN: C=US,ST=California,L=Los Angeles,O=Verizon Digital Media Services\, Inc.,CN=www.example.org
[...]
----
  [0]         Version: 3
         SerialNumber: 9101305761976670746388865003982847684
             IssuerDN: C=US,O=DigiCert Inc,OU=www.digicert.com,CN=DigiCert Global Root CA
           Start Date: 14/04/2021 00:00:00
           Final Date: 13/04/2031 23:59:59
            SubjectDN: C=US,O=DigiCert Inc,CN=DigiCert TLS RSA SHA256 2020 CA1
[...]
----
  [0]         Version: 3
         SerialNumber: 10944719598952040374951832963794454346
             IssuerDN: C=US,O=DigiCert Inc,OU=www.digicert.com,CN=DigiCert Global Root CA
           Start Date: 10/11/2006 00:00:00
           Final Date: 10/11/2031 00:00:00
            SubjectDN: C=US,O=DigiCert Inc,OU=www.digicert.com,CN=DigiCert Global Root CA
[...]
```

## Installation

Download macOS, Linux or Windows executables from the release page.
Copy the file somewhere in you PATH.

## Development

This is early days, code is a mess and just barely usable. Feel free to put
issues and feature requests in but putting a PR in won't probably make
sense now.

