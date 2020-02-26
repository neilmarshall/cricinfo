const validateInputAsync = function (validationEndpoint) {
    return function (e) {
        const validateInput = function (target) {
            $.get(
                validationEndpoint,
                { 'data': target.value },
                response => {
                    if (response) {
                        target.classList.add('is-valid');
                        target.classList.remove('is-invalid');
                    } else {
                        target.classList.remove('is-valid');
                        target.classList.add('is-invalid');
                    }
                }
            );
        };

        if (!e.target.value) {
            e.target.classList.remove('is-valid');
            e.target.classList.remove('is-invalid');
        }
        else {
            validateInput(e.target);
        }
    };
};
