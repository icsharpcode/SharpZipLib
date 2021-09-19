const https = require('https')

const authorUsers = {}

/** 
 * @param {string} email 
 * @param {string} prId 
 * @returns {Promise<string | null>} User login if found */
const getAuthorUser = async (email, prId) => {
	const lookupUser = authorUsers[email];
	if (lookupUser) return lookupUser;

	const match = /[0-9]+\+([^@]+)@users\.noreply\.github\.com/.exec(email);
	if (match) {
		return match[1];
	}

    const pr = await new Promise((resolve, reject) => {
        console.warn(`Looking up GitHub user for PR #${prId} (${email})...`)
        https.get(`https://api.github.com/repos/icsharpcode/sharpziplib/pulls/${prId}`, {
            headers: {Accept: 'application/vnd.github.v3+json', 'User-Agent': 'release-notes-script/0.3.1'}
        }, (res) => {
            res.setEncoding('utf8');
            let chunks = '';
            res.on('data', (chunk) => chunks += chunk);
            res.on('end', () => resolve(JSON.parse(chunks)));
            res.on('error', reject);
        }).on('error', reject);
    }).catch(e => {
        console.error(`Could not get GitHub user (${email}): ${e}}`)
        return null;
    });

	if (!pr) {
		console.error(`Could not get GitHub user (${email})}`)
		return null;
	} else {
		const user = pr.user.login;
		console.warn(`Resolved email ${email} to user ${user}`)
		authorUsers[email] = user;
		return user;	
	}
}

/** 
 * @typedef {{issue?: string, sha1: string, authorEmail: string, title: string, type: string}} Commit
 * @param {{commits: Commit[], range: string, dateFnsFormat: ()=>any, debug: (...p[]) => void}} data 
 * @param {(data: {commits: Commit[], extra: {[key: string]: any}}) => void} callback
 * */
module.exports = (data, callback) => {
	// Migrates commits in the old format to conventional commit style, omitting any commits in neither format
	const normalizedCommits = data.commits.flatMap(c => {
		if (c.type) return [c]
		const match = /^(?:Merge )?(?:PR ?)?#(\d+):? (.*)/.exec(c.title)
		if (match != null) {
			const [, issue, title] = match
			return [{...c, title, issue, type: '?'}]
		} else {
			console.warn(`Skipping commit [${c.sha1.substr(0, 7)}] "${c.title}"!`);
			return [];
		}
	});

	const commitAuthoredBy = email => commit => commit.authorEmail === email && commit.issue ? [commit.issue] : []
	const authorEmails = new Set(normalizedCommits.map(c => c.authorEmail));
	Promise.all(
		Array
		.from(authorEmails.values(), e => [e, normalizedCommits.flatMap(commitAuthoredBy(e))])
		.map(async ([email, prs]) => [email, await getAuthorUser(email, ...prs)])
	)
	.then(Object.fromEntries)
	.then(authorUsers => callback({
		commits: normalizedCommits.map(c => ({...c, authorUser: authorUsers[c.authorEmail]})),
		extra: {}
	}))
};
