package org.itadaki.bzip2;

import java.io.IOException;

/**
 * Indicates that a data format error was encountered while attempting to decode bzip2 data
 */
public class BZip2Exception extends IOException {

	/**
	 * Serial version UID
	 */
	private static final long serialVersionUID = -8931219115669559570L;

	/**
	 * @param reason The exception's reason string
	 */
	public BZip2Exception (String reason) {

		super (reason);

	}

}
