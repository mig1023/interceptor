#!/usr/bin/perl
use strict;

use lib '/usr/local/www/data/htdocs/vcs/lib';

use VCS::Config;
use VCS::Vars;
use VCS::SQL;
use VCS::System;
use VCS::Memcache;
use LWP;
use HTTP::Cookies;
use HTTP::Headers;
use HTTP::Request::Common;
use Digest::MD5  qw(md5_hex);
use Digest::SHA1  qw(sha1_hex);
use Data::Dumper;
use Encode qw(decode encode);

	my $serv = '127.0.0.1';

	my $services = [
	
		{
			Name	=> 'Сервисный сбор',
			Number	=> 1,
			Price	=> '2000.00',
			Total	=> '2000.00',
			VAT	=> '360.00',
		},
		{
			Name	=> 'Консульский сбор',
			Number	=> 1,
			Price	=> '1500.00',
			Total	=> '1500.00',
			VAT	=> '270.00',
		},
	];
	
	my $info = {
		AgrNumber => '01.000209.091418',
		Cashier => 'Иванов И.И.',
		MoneyType => 1,
		Total => '3500.00',
		Money => '5000.00',
		Change => '3000.00',
	};

	#for ( @$req ) {
	
		my $request = xmlCreate( $services, $info );
		
		# print "запрос: " . Dumper( $request )."\n\n";
		
		my $resp = sendRequest( $request );
		
		print "получен ответ: $resp\n\n";
	#}

sub xmlCreate
{
	my ( $services, $info ) = @_;
	
	
	my $md5line = '';
	my $xml = '<Services>';
	
	for my $service ( @$services ) {
	
		$xml .= '<Service>';
	
		for my $field ( sort { $a cmp $b } keys %$service ) {
		
			$xml .= "<$field>" . $service->{ $field } . "</$field>";
			
			$md5line .= $service->{ $field };
		}
		
		$xml .= '</Service>';
	}
	
	$xml .= '</Services>';
	
	$xml .= '<Info>';
	
	for ( sort { $a cmp $b } keys %$info ) {
	
		$xml .= "<$_>" . $info->{ $_ } . "</$_>";
		
		$md5line .= $info->{ $_ };
	}
	
	$xml .= '</Info>';
	
	my $currentDate = getDate();
	
	$md5line .= $currentDate;
	
	Encode::from_to( $md5line, 'utf-8', 'windows-1251' );
	
	my $bytecode = "";
	
	$bytecode .= ord($_) . " " for split( //, $md5line );
	
	my $md5 = Digest::MD5->new->add( $bytecode )->hexdigest;
	
	$xml =  '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			$xml .
			'<Control>' .
				'<CRC>' . $md5 . '</CRC>' . 
				'<Date>' . $currentDate . '</Date>' .
			'</Control>' .
		'</toCashbox>';

	return $xml;
}

sub sendRequest
{
	my $line = shift;
	
	my $ua = LWP::UserAgent->new;
	
	$ua->agent('Mozilla/4.0 (compatible; MSIE 6.0; X11; Linux i686; en) Opera 7.60');
	
	my $request = HTTP::Request->new(GET => $serv.'/?message='.$line.';');

	my $response = $ua->request($request);
	
	return $response->{ _content };
}

sub getDate
{
	my ( $sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst ) = localtime( time );
	
	$year += 1900;
	$mon++;
	
	for ( $sec, $min, $hour, $mday, $mon, $year ) { 
	
		$_ = '0'.$_ if $_ < 10;
	};
	
	return "$year-$mon-$mday $hour:$min:$sec";
}
