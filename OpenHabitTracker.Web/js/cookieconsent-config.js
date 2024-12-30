import 'https://cdn.jsdelivr.net/gh/orestbida/cookieconsent@3.0.1/dist/cookieconsent.umd.js';

// Enable dark mode
document.documentElement.classList.add('cc--darkmode');

CookieConsent.run({
    guiOptions: {
        consentModal: {
            layout: "box",
            position: "bottom left",
            equalWeightButtons: false,
            flipButtons: false
        },
        preferencesModal: {
            layout: "box",
            position: "right",
            equalWeightButtons: false,
            flipButtons: false
        }
    },
    categories: {
        necessary: {
            readOnly: true
        },
        analytics: {}
    },
    language: {
        default: "en",
        translations: {
            en: {
                consentModal: {
                    title: "We Value Your Privacy",
                    description: "We use cookies to enhance your browsing experience and analyze our traffic.",
                    closeIconLabel: "",
                    acceptAllBtn: "Accept all",
                    acceptNecessaryBtn: "Reject all",
                    showPreferencesBtn: "Manage preferences",
                    footer: "<a href=\"privacy-policy.html\">Privacy Policy</a>"
                },
                preferencesModal: {
                    title: "Consent Preferences",
                    closeIconLabel: "Close modal",
                    acceptAllBtn: "Accept all",
                    acceptNecessaryBtn: "Reject all",
                    savePreferencesBtn: "Save preferences",
                    serviceCounterLabel: "Service|Services",
                    sections: [
                        {
                            title: "Cookie Usage",
                            description: "Cookies are used on this website to improve your browsing experience and analyze site traffic."
                        },
                        {
                            title: "Essential Cookies <span class=\"pm__badge\">Always Enabled</span>",
                            description: "These cookies are necessary for the website to function properly.",
                            linkedCategory: "necessary"
                        },
                        {
                            title: "Analytics Cookies",
                            description: "These cookies help us understand how visitors interact with the website by collecting and reporting information anonymously.",
                            linkedCategory: "analytics"
                        }
                    ]
                }
            }
        }
    }
});