# Security

Do not post credentials, PSN tokens, device serials or unredacted diagnostics in public issues. Use GitHub private vulnerability reporting when enabled. For non-sensitive bugs, use an ordinary issue.

EvoBridge's local backend must remain bound to loopback. Dependency downloads are hash checked. Driver installation is a separate user action. Avoid running unknown modified builds with administrator privileges.

If input is left hidden after a crash, close EvoBridge and run the included restoration command from the same folder. Preserve its hiding journal until restoration succeeds.
