(function () {

    if (!window.slicito) {

        // Associative array from site GUID to its state
        let siteStates = {};

        let getOrCreateSiteState = function (siteGuid) {
            let siteState = siteStates[siteGuid];
            if (!siteState) {
                siteState = {
                    history: []
                };

                siteStates[siteGuid] = siteState;
            }

            return siteState;
        }

        let getSiteElement = function (siteGuid) {
            return document.getElementById("slicito-site-" + siteGuid);
        };

        let getBackButtonElement = function (siteGuid) {
            let siteElement = getSiteElement(siteGuid);

            return siteElement.getElementsByClassName("slicito-site-back")[0];
        }

        let getContentElement = function (siteGuid) {
            return document.getElementById("slicito-site-content-" + siteGuid);
        };

        let extractLinkHref = function (a) {
            if (a.href instanceof String) {
                return a.href;
            } else {
                return a.href.baseVal;  // SVGAnimatedString
            }
        };

        window.slicito = {

            forwardLinksToDotNet: function (siteGuid) {
                let siteContentElement = getContentElement(siteGuid);

                let linkElements = siteContentElement.getElementsByTagName("a");
                for (let j = 0; j < linkElements.length; j++) {
                    let linkElement = linkElements[j];
                    let href = extractLinkHref(linkElement);

                    let destinationGuid = href.replace("#", "");

                    linkElement.addEventListener("click", function () {
                        let jsKernel = kernel.root.findKernelByName("javascript");

                        // We must send the command to .NET kernel in the context of a JavaScript command, otherwise it gets stuck
                        jsKernel.send({
                            commandType: 'SubmitCode',
                            command: {
                                code: 'window.slicito.requestSiteNavigation("' + siteGuid + '", "' + destinationGuid + '");'
                            }
                        });
                    });
                }
            },

            requestSiteNavigation: function (siteGuid, destinationGuid) {
                let csharpKernel = kernel.root.findKernelByName("csharp");
                if (!csharpKernel) {
                    return;
                }

                csharpKernel.send({
                    commandType: 'SubmitCode',
                    command: {
                        code: 'await Slicito.Interactive.InteractiveSession.Global.NavigateSiteToAsync(new("' + siteGuid + '"), new("' + destinationGuid + '"));'
                    }
                });
            },

            showSiteContent: function (siteGuid, content) {
                let history = getOrCreateSiteState(siteGuid).history;
                let backButtonElement = getBackButtonElement(siteGuid);
                let siteContentElement = getContentElement(siteGuid);

                history.push(siteContentElement.innerHTML);
                backButtonElement.removeAttribute("style");

                siteContentElement.innerHTML = content;
                this.forwardLinksToDotNet(siteGuid);
            },

            showPreviousSiteContent: function (siteGuid) {
                let history = getOrCreateSiteState(siteGuid).history;
                let backButtonElement = getBackButtonElement(siteGuid);
                let siteContentElement = getContentElement(siteGuid);

                let content = history.pop();
                if (history.length == 0) {
                    backButtonElement.setAttribute("style", "display: none;");
                }

                siteContentElement.innerHTML = content;
                this.forwardLinksToDotNet(siteGuid);
            }
        };
    }

})();
