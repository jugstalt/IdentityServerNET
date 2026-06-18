// WebAuthn / Passkey browser helpers for IdentityServer.NET
// Handles both attestation (registration) and assertion (sign-in) flows.

(function (PasskeyUI) {

    // ---------- Base64Url helpers ----------

    function base64UrlToBytes(base64url) {
        var b64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
        while (b64.length % 4 !== 0) b64 += '=';
        var bin = window.atob(b64);
        var bytes = new Uint8Array(bin.length);
        for (var i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
        return bytes;
    }

    function bytesToBase64Url(bytes) {
        var bin = '';
        bytes.forEach(function (b) { bin += String.fromCharCode(b); });
        return window.btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
    }

    // Convert all ArrayBuffer / Uint8Array leaves in an object returned by
    // the WebAuthn API to Base64Url strings so they can be JSON-serialised.
    function encodeCredential(cred) {
        var obj = {
            id: cred.id,
            rawId: bytesToBase64Url(new Uint8Array(cred.rawId)),
            type: cred.type,
            clientExtensionResults: (typeof cred.getClientExtensionResults === 'function')
                ? cred.getClientExtensionResults()
                : {},
            response: {}
        };
        var r = cred.response;
        if (r.attestationObject)
            obj.response.attestationObject = bytesToBase64Url(new Uint8Array(r.attestationObject));
        if (r.clientDataJSON)
            obj.response.clientDataJSON = bytesToBase64Url(new Uint8Array(r.clientDataJSON));
        if (r.authenticatorData)
            obj.response.authenticatorData = bytesToBase64Url(new Uint8Array(r.authenticatorData));
        if (r.signature)
            obj.response.signature = bytesToBase64Url(new Uint8Array(r.signature));
        if (r.userHandle && r.userHandle.byteLength > 0)
            obj.response.userHandle = bytesToBase64Url(new Uint8Array(r.userHandle));
        if (r.transports && typeof r.getTransports === 'function')
            obj.response.transports = r.getTransports();
        return obj;
    }

    // Decode the JSON options coming from the server: Base64Url strings → ArrayBuffers.
    function prepareCreationOptions(json) {
        var options = typeof json === 'string' ? JSON.parse(json) : json;
        options.challenge = base64UrlToBytes(options.challenge);
        if (options.user && options.user.id)
            options.user.id = base64UrlToBytes(options.user.id);
        if (options.excludeCredentials) {
            options.excludeCredentials = options.excludeCredentials.map(function (c) {
                return Object.assign({}, c, { id: base64UrlToBytes(c.id) });
            });
        }
        return options;
    }

    function prepareRequestOptions(json) {
        var options = typeof json === 'string' ? JSON.parse(json) : json;
        options.challenge = base64UrlToBytes(options.challenge);
        if (options.allowCredentials) {
            options.allowCredentials = options.allowCredentials.map(function (c) {
                return Object.assign({}, c, { id: base64UrlToBytes(c.id) });
            });
        }
        return options;
    }

    // ---------- Public API ----------

    /**
     * Register a new passkey (attestation).
     * @param {string|object} optionsJson  JSON from /Account/PasskeyCreationOptions
     * @returns {Promise<string>}          JSON to POST to /Account/PasskeyRegister
     */
    PasskeyUI.register = async function (optionsJson) {
        var options = prepareCreationOptions(optionsJson);
        var cred = await navigator.credentials.create({ publicKey: options });
        return JSON.stringify(encodeCredential(cred));
    };

    /**
     * Authenticate with a passkey (assertion / sign-in).
     * @param {string|object} optionsJson  JSON from /Account/PasskeyChallenge
     * @returns {Promise<string>}          JSON to POST to /Account/PasskeySignIn
     */
    PasskeyUI.authenticate = async function (optionsJson) {
        var options = prepareRequestOptions(optionsJson);
        var cred = await navigator.credentials.get({ publicKey: options });
        return JSON.stringify(encodeCredential(cred));
    };

    /**
     * Full sign-in flow wired to button + hidden form fields.
     * @param {string} challengeUrl    URL to GET assertion options JSON
     * @param {string} assertionField  id of the hidden <input> to populate
     * @param {string} formId          id of the <form> to submit
     */
    PasskeyUI.signIn = async function (challengeUrl, assertionField, formId) {
        try {
            var resp = await fetch(challengeUrl, { cache: 'no-store' });
            if (!resp.ok) throw new Error('Challenge request failed: ' + resp.status);
            var optionsJson = await resp.text();
            var assertionJson = await PasskeyUI.authenticate(optionsJson);
            document.getElementById(assertionField).value = assertionJson;
            document.getElementById(formId).submit();
        } catch (err) {
            alert('Passkey sign-in failed: ' + err.message);
        }
    };

    /**
     * Full registration flow wired to button + hidden form fields.
     * @param {string} optionsUrl      URL to GET attestation options JSON
     * @param {string} attestationField  id of the hidden <input> to populate
     * @param {string} formId            id of the <form> to submit
     */
    PasskeyUI.registerNew = async function (optionsUrl, attestationField, formId) {
        try {
            var resp = await fetch(optionsUrl, { cache: 'no-store' });
            if (!resp.ok) throw new Error('Options request failed: ' + resp.status);
            var optionsJson = await resp.text();
            var attestationJson = await PasskeyUI.register(optionsJson);
            document.getElementById(attestationField).value = attestationJson;
            document.getElementById(formId).submit();
        } catch (err) {
            alert('Passkey registration failed: ' + err.message);
        }
    };

    // ---------- Auto-wiring (no inline onclick needed) ----------

    document.addEventListener('DOMContentLoaded', function () {

        // Login page: passwordless sign-in button
        var signInBtn = document.getElementById('passkey-signin-btn');
        if (signInBtn) {
            signInBtn.addEventListener('click', function () {
                PasskeyUI.signIn(
                    this.dataset.challengeUrl,
                    'passkey-assertion-json',
                    'passkey-signin-form'
                );
            });
        }

        // LoginWithPasskey page: second-factor verify button
        var verifyBtn = document.getElementById('passkey-verify-btn');
        if (verifyBtn) {
            verifyBtn.addEventListener('click', function () {
                PasskeyUI.signIn(
                    this.dataset.challengeUrl,
                    this.dataset.field,
                    this.dataset.form
                );
            });
        }

        // Manage/Passkeys page: register new passkey button
        var registerBtn = document.getElementById('passkey-register-btn');
        if (registerBtn) {
            registerBtn.addEventListener('click', function () {
                PasskeyUI.registerNew(
                    this.dataset.optionsUrl,
                    this.dataset.field,
                    this.dataset.form
                );
            });
        }

        // Remove buttons: data-confirm replaces onclick="return confirm(...)"
        document.querySelectorAll('[data-confirm]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                if (!confirm(this.dataset.confirm)) {
                    e.preventDefault();
                }
            });
        });

    });

}(window.PasskeyUI = window.PasskeyUI || {}));
