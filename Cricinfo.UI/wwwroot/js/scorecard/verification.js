let scorecard = function() {

    document.getElementById("innings-scorecard-0").classList.add("show");

    const IDs = [...document.querySelectorAll(".innings-scorecard")].map(e => e.id);
    const MAX_ID = IDs.length - 1;

    const setScorecardButtonStatus = function (cur_idx, nxt_idx) {
        if (cur_idx === 0 && nxt_idx === 1 || cur_idx === 1 && nxt_idx === 0) {
            document.getElementById("previous-innings-btn").toggleAttribute("disabled");
        }
        if (cur_idx === MAX_ID && nxt_idx === MAX_ID - 1 || cur_idx === MAX_ID - 1 && nxt_idx === MAX_ID) {
            document.getElementById("next-innings-btn").toggleAttribute("disabled");
        }
    };

    const updateScorecard = function (getNextId) {
        const currentScorecardId = document.querySelectorAll(".innings-scorecard.show")[0].id;
        const currentScorecardIndex = IDs.indexOf(currentScorecardId);
        const nextScorecardIndex = getNextId(currentScorecardIndex) % IDs.length
        const nextScorecardId = `innings-scorecard-${nextScorecardIndex}`;
        document.getElementById(`${currentScorecardId}`).classList.toggle("show");
        document.getElementById(`${nextScorecardId}`).classList.toggle("show");
        setScorecardButtonStatus(currentScorecardIndex, nextScorecardIndex);
    };

    const showPreviousScorecard = function () { updateScorecard(idx => idx - 1) };
    const showNextScorecard = function () { updateScorecard(idx => idx + 1); };

    document.getElementById("previous-innings-btn").addEventListener("click", showPreviousScorecard);
    document.getElementById("next-innings-btn").addEventListener("click", showNextScorecard);
}