# Check X.509 Certificates for Sites

`chkcert` is a simple utility to show certificate information for a given web
site helping you to debug certificate issues you might have.

Usage:
```
chkcert [-vvv] <url|host>
```

## Example

List the certificate chain:
```
$ chkcert example.com
https://example.com
 Subject: CN=www.example.org, O="Verizon Digital Media Services, Inc.", L=Los Angeles, S=California, C=US
  Issuer: CN=DigiCert TLS RSA SHA256 2020 CA1, O=DigiCert Inc, C=US

 Subject: CN=DigiCert TLS RSA SHA256 2020 CA1, O=DigiCert Inc, C=US
  Issuer: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US

 Subject: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
  Issuer: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
```

Also display alternative names:
```
$ chkcert -v example.com
https://example.com
 Subject: CN=www.example.org, O="Verizon Digital Media Services, Inc.", L=Los Angeles, S=California, C=US
  Issuer: CN=DigiCert TLS RSA SHA256 2020 CA1, O=DigiCert Inc, C=US
  Others: www.example.org example.net example.edu example.com example.org www.example.com www.example.edu
          www.example.net

 Subject: CN=DigiCert TLS RSA SHA256 2020 CA1, O=DigiCert Inc, C=US
  Issuer: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US

 Subject: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
  Issuer: CN=DigiCert Global Root CA, OU=www.digicert.com, O=DigiCert Inc, C=US
```
Increase verbosity upto three `-vvv` to get more details.

Also follows redirects:
```
$ chkcert google.com
https://google.com
 Subject: CN=*.google.com
  Issuer: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US

 Subject: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US

 Subject: CN=GTS Root R1, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US

https://www.google.com/
 Subject: CN=www.google.com
  Issuer: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US

 Subject: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US

 Subject: CN=GTS Root R1, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US

https://consent.google.com/ml?continue=https://www.google.com/&gl=GB&m=0&pc=shp&hl=en&src=1
 Subject: CN=*.google.com
  Issuer: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US

 Subject: CN=GTS CA 1C3, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US

 Subject: CN=GTS Root R1, O=Google Trust Services LLC, C=US
  Issuer: CN=GTS Root R1, O=Google Trust Services LLC, C=US
```

## Installation

Download macOS, Linux or Windows executables from the
[releases page](https://github.com/mtmk/chkcert/releases).
Copy the file somewhere in you PATH.

## Development

This is early days, code is a mess and just barely usable. Feel free to put
issues and feature requests in but putting a PR in won't probably make
sense now.

