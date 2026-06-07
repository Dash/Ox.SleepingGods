<?php
date_default_timezone_set('UTC');
$uid = $_SERVER['PATH_INFO'] ?? null;

// If no id is provided, bail
if($uid == null || strlen($uid) <= 1)
{
	http_response_code(404);
	die;
}

$uid = substr($uid, 1);

// CORS headers
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Headers: *');
header('Access-Control-Expose-Headers: *');
header('Access-Control-Allow-Methods: OPTIONS, GET, PUT');

// Route requests
switch($_SERVER['REQUEST_METHOD'])
{
	case 'OPTIONS':
		http_response_code(204);
		header('Allow: OPTIONS, GET, PUT');
		die;
		break;
	case 'GET':
		HandleGet($uid);
		break;
	case 'PUT':
		HandlePut($uid);
		break;
}

/**
 * Builds a file path for the database file
 * @param string $uid Database unique identifier from path
 */
function BuildPath(string $uid)
{
	if(str_contains($uid, '..') || str_contains($uid, '/'))
		throw new \Exception('Invalid file name');

	return 'store/' . $uid . '.json';

}

/**
 * Processes a fetch of the database, supports etags to avoid sending the database when no changes have been made.
 * @param string $uid Database unique identifier from path
 */
function HandleGet(string $uid)
{
	$path = BuildPath($uid);

	// If we don't hold a copy of this database, return a 404.
	if(!file_exists($path))
	{
		http_response_code(404);
		die;
	}

	// Set the etag based on filetimestamp
	$age = filemtime($path);
	header(sprintf('ETag: "%d"', $age));
	
	// Assess whether the file needs to be returned, or if the same as the client's
	$etag = $_SERVER['HTTP_IF_NONE_MATCH'] ?? null;
	if($etag != null)
	{
		$etag = substr($etag, 1, strlen($etag) - 2);
		if($age == $etag)
		{
			http_response_code(304);
			die;
		}
	}

	// Set the headers for the file download
	header('Content-Type: application/json');
	header(sprintf('Content-Disposition: inline; filename="%s.json"', $uid));
	header('Content-Length: ' . filesize($path));

	// Stream the file
	$fp = fopen($path, 'r');
	fpassthru($fp);
	fclose($fp);
}

/**
 * Handles receiving a copy of the database for saving locally.  No validation is done, whatever is pushed is blindly
 * accepted as being the latest.  The assumption is proper use of a previous GET.
 * @param string $uid Database unique identifier from path
 */
function HandlePut(string $uid)
{
	// Simple check for massive files
	if($_SERVER['CONTENT_LENGTH'] > 10240)
	{
		http_response_code(400);
		echo "File size too large";
		die;
	}

	// Save file to disk
	$path = BuildPath($uid);
	file_put_contents($path, file_get_contents('php://input'));

	// Try to keep the age in sync with the request
	$age = $_SERVER['REQUEST_TIME'];
	touch($path, mtime: $age);

	header(sprintf('ETag: "%d"', $age));

	http_response_code(202);
}

?>