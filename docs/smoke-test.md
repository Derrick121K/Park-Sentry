# Production smoke test

Repeat for Organization A and Organization B.

1. Open Web over HTTPS
2. Login
3. Confirm organization context
4. Dashboard loads
5. Guard → Scan Vehicle
6. Camera permission path (or document hardware skip)
7. Manual registration entry works
8. Vehicle lookup + watchlist check
9. Select available bay
10. Entry creates session
11. Bay occupied + SignalR update (Web path)
12. Active sessions list shows session
13. Exit + payment per configured provider
14. Session closed; bay available
15. Audit log contains entry/exit/payment
16. Confirm Org A cannot see Org B data

Record environment, provider modes, and what was/wasn't hardware-verified.
