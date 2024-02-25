var skipScrollTo = false;
const origScrollTo = window.scrollTo;
window.scrollTo = (x, y) => {
    if (x === 0 && y === 0 && skipScrollTo) {
        skipScrollTo = false;
        return;
    }
    return origScrollTo.apply(this, [x, y]);
};

function skipNextScrollTo() {
    skipScrollTo = true;
}
