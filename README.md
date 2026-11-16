iTextSharp (LGPL, Bouncy Castle)
================================

This project is a fork of [iTextSharp (LGPL / MPL) 4.1.6](https://github.com/Sbaia/iTextSharp-LGPL-BouncyCastle) that includes [Bouncy Castle](https://github.com/bcgit/bc-csharp) as a git submodule instead of directly including the source code.

Also you can use Bouncy Castle as nuget package instead of referenced sub module

## Changes

In this version complete html parser and xmp, xml and simmilar parsers are stripped. Using this version you can no longer utilize html and xml functionalities.

Remote URL resources are fetched only over HTTPS. Hosts resolving to non-public IP addresses are rejected, redirects are validated, requests have a 15-second timeout, and response bodies are limited to 64 MiB. Explicit local filename APIs remain available; `file:` URLs and non-HTTPS URLs are rejected.
