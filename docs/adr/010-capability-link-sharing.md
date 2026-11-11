# ADR-010: Capability-Link Sharing with the Key in the URL Fragment

## Status
Accepted

## Context
Evidence is worth little if it cannot be handed to a lawyer, a journalist, a family member, or an oversight body. Those recipients will not install an app or create an account, and several will be on a borrowed device.

Under end-to-end encryption this is genuinely awkward: the recipient needs the content key, and the service must never have it.

## Decision
Share by capability link:

```
https://app.example/v/{artifactId}#k={key material}&t={access token}
```

- The **fragment is never transmitted to the server** — browsers do not send it — so the key travels only to the recipient's browser, which decrypts locally using the same `Evidence` library that wrote the file.
- The **token** authorizes ciphertext retrieval and is independently expiring, revocable, scoped to specific artifacts, and access-logged.
- Because key and authorization are separate, revoking access does not require the key to have stayed secret, and possessing the key alone does not grant retrieval.
- The recipient can run in-browser verification of the hash chain and signatures without an account (ADR-007).

## Consequences
- (+) Sharing works for people with no account, no app, and no technical skill, without ever giving the service the ability to read the content.
- (+) Revocation, expiry and access logs give the user real control after the fact.
- (−) Anyone who obtains the full URL obtains the content until the token is revoked. Links must be treated as bearer credentials, and the UI must say so plainly.
- (−) Fragment-based keys can leak through browser history, screenshots, and careless forwarding. Short default expiry and a visible active-links screen mitigate; nothing eliminates.
- (−) Server access logs record who fetched ciphertext and when — metadata that the sharing user should be told about.

## Alternatives Considered
- **Recipient accounts with key wrapping**: strongest control, and it fails the actual use case — the recipient will not sign up mid-crisis.
- **Server-side decryption for shared items**: trivial UX, and it would hand the service exactly the capability the whole architecture exists to deny it.
- **Password-protected shares** (key derived from a passphrase sent out-of-band): stronger against link leakage, weaker in practice because it depends on a second secure channel the user may not have.
