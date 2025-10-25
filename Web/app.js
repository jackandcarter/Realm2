(() => {
  const defaultConfig = { API_BASE_URL: 'http://localhost:3000' };
  const runtimeConfig = window.REALM_PORTAL_CONFIG ?? {};
  const config = { ...defaultConfig, ...runtimeConfig };
  const apiBaseUrl = config.API_BASE_URL.replace(/\/$/, '');

  const form = document.getElementById('registration-form');
  const feedback = document.querySelector('.feedback');
  const submitButton = form.querySelector('button[type="submit"]');
  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear().toString();
  }

  function setFeedback(message, type = 'info') {
    if (!feedback) return;
    feedback.textContent = message;
    feedback.classList.remove('feedback--success', 'feedback--error');
    if (type === 'success') {
      feedback.classList.add('feedback--success');
    } else if (type === 'error') {
      feedback.classList.add('feedback--error');
    }
  }

  function validateInputs(email, username, password) {
    const errors = [];
    if (!email) {
      errors.push('Email is required.');
    }

    if (!username) {
      errors.push('Username is required.');
    } else if (!/^[a-zA-Z0-9_\-]{3,20}$/.test(username)) {
      errors.push('Username must be 3-20 characters using letters, numbers, underscores, or hyphens.');
    }

    if (!password) {
      errors.push('Password is required.');
    } else {
      if (password.length < 8) {
        errors.push('Password must be at least 8 characters long.');
      }
      if (!/[A-Z]/.test(password)) {
        errors.push('Password must include an uppercase letter.');
      }
      if (!/[a-z]/.test(password)) {
        errors.push('Password must include a lowercase letter.');
      }
      if (!/[0-9]/.test(password)) {
        errors.push('Password must include a number.');
      }
      if (!/[^A-Za-z0-9]/.test(password)) {
        errors.push('Password must include a special character.');
      }
    }

    return errors;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setFeedback('');

    const formData = new FormData(form);
    const email = formData.get('email')?.toString().trim().toLowerCase();
    const username = formData.get('username')?.toString().trim();
    const password = formData.get('password')?.toString();

    const errors = validateInputs(email, username, password);
    if (errors.length > 0) {
      setFeedback(errors[0], 'error');
      return;
    }

    submitButton.disabled = true;
    submitButton.textContent = 'Creating account...';

    try {
      const response = await fetch(`${apiBaseUrl}/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, username, password }),
      });

      if (!response.ok) {
        let errorMessage = 'Unable to create account. Please try again.';
        try {
          const errorBody = await response.json();
          if (errorBody?.message) {
            errorMessage = errorBody.message;
          }
        } catch (error) {
          // ignore json parse error
        }
        throw new Error(errorMessage);
      }

      const result = await response.json();
      const { user } = result;
      setFeedback(`Welcome, ${user.username}! Your account is ready.`, 'success');
      form.reset();
    } catch (error) {
      setFeedback(error.message, 'error');
    } finally {
      submitButton.disabled = false;
      submitButton.textContent = 'Create account';
    }
  }

  form.addEventListener('submit', handleSubmit);
})();
