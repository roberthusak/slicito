let tryFindLinkEventTarget = function(e) {
    let path = e.composedPath();

    for (let i = 0; i < path.length; i++) {
        const element = path[i];
        
        if (element.tagName == 'a') {
            return element;
        }
    }

    return null;
};

let extractLinkHref = function(a) {
    if (a.href instanceof String) {
        return a.href;
    } else {
        return a.href.baseVal;  // SVGAnimatedString
    }
}

document.onclick = function(e) {
    e = e ||  window.event;

    let a = tryFindLinkEventTarget(e);
    if (a) {
        window.alert(extractLinkHref(a));
        return false; // prevent default action and stop event propagation
    }
};