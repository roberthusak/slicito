(function () {

    if (!window.slicito) {

        let getContentElement = function (siteGuid) {
            return siteContentElement = document.getElementById("slicito-site-content-" + siteGuid);
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
                let siteContentElement = getContentElement(siteGuid);
                siteContentElement.innerHTML = content;
                this.forwardLinksToDotNet(siteGuid);
            }
        };
    }

})();
