#!/usr/bin/perl
use strict;

use lib '/usr/local/www/data/htdocs/vcs/lib';

use VCS::Config;
use LWP;
use Digest::MD5  qw(md5_hex);
use Data::Dumper;
use Encode qw(decode encode);

	my $serv = '127.0.0.1';

	
	    # 1 - сервисный сбор (92101) киев
            # 2 - срочн.сервисный (92111) киев
            # 3 - ВИП комфорт
            # 4 - ВИП обслуживание
            # 5 - Доставка
            # 6 - КС
            # 7 - СМС
            # 8 - ВИП Стандарт
            # 9 - сервисный сбор (00101) мск
            # 10 - срочн.сервисный (00111) мск
            # 11 - страхование
            # 12 - доставка
            # 13 - анкета
            # 14 - распечатка
            # 15 - фотопечать
            # 16 - ксерокс
	
	my $services = [
	
		{
			Name		=> 'Сервисный сбор',
			ServiceID	=> 9,
			Quantity	=> 2,
			Price		=> '20.00',
			VAT		=> 1,
		},
		{
			Name		=> 'Консульский сбор',
			ServiceID	=> 6,
			Quantity	=> 1,
			Price		=> '15.00',
			VAT		=> 0,
		},
		{
			Name		=> 'СМС',
			ServiceID	=> 7,
			Quantity	=> 3,
			Price		=> '4.00',
			VAT		=> 1,
		},
		{
			Name		=> 'Страхование',
			ServiceID	=> 11,
			Quantity	=> 1,
			Price		=> '10.00',
			VAT		=> 0,
		},
		{
			Name		=> 'Доставка',
			ServiceID	=> 12,
			Quantity	=> 1,
			Price		=> '3.50',
			VAT		=> 1,
		},
		{
			Name		=> 'VIP',
			ServiceID	=> 4,
			Quantity	=> 1,
			Price		=> '5.50',
			VAT		=> 1,
		},
	];
	
	my $info = {
		AgrNumber => '01.000209.091418',
		Cashier => 'Иванов И.И.',
		CashierPass => 26,
		
		MoneyType => 1, # 1 - наличка, 2 - карта
		Total => '169.00',
		Money => '200.00',
	};

	my $request = xmlCreate( $services, $info );
	
	my $resp = sendRequest( $request );
	
	print "получен ответ: $resp\n\n";
	

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
